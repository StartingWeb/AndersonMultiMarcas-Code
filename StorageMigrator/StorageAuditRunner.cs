using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Core.Storage;
using Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Project.Features.Storage.Legacy;

namespace StorageMigrator;

public sealed class StorageAuditRunner(
    ApplicationDbContext db,
    R2StorageService r2,
    IOptions<StorageOptions> storageOptions,
    IOptions<StorageAuditOptions> auditOptions,
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<StorageAuditRunner> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private static readonly byte[] ProbePngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=");

    public async Task<int> RunAsync(CancellationToken ct)
    {
        var options = auditOptions.Value;
        if (!r2.IsConfigured)
        {
            logger.LogError("Cloudflare R2 nao esta configurado. Informe Storage:R2.");
            return 2;
        }

        logger.LogInformation("Auditoria de storage iniciada. Prefix={Prefix}; PublicUrls={PublicUrls}",
            string.IsNullOrWhiteSpace(options.Prefix) ? "(bucket inteiro)" : NormalizeOptionalPrefix(options.Prefix),
            options.ValidatePublicUrls);

        var bucketKeys = await ListBucketKeysAsync(options.Prefix, ct);
        var mediaRecords = await LoadMediaRecordsAsync(ct);
        var latestJobs = await LoadLatestJobsAsync(ct);
        var resolvedRecords = mediaRecords.Select(ResolveStorageKey).ToList();
        var distinctKeys = resolvedRecords
            .Where(x => x.HasBlobName && !string.IsNullOrWhiteSpace(x.StorageKey))
            .Select(x => x.StorageKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var metadata = await ValidateMetadataAsync(distinctKeys, ct);
        var publicUrls = options.ValidatePublicUrls
            ? await ValidatePublicUrlsAsync(distinctKeys, ct)
            : new Dictionary<string, PublicUrlAudit>(StringComparer.OrdinalIgnoreCase);

        var summary = BuildSummary(mediaRecords, resolvedRecords, bucketKeys, metadata, publicUrls);
        var orphanVehicleIds = resolvedRecords
            .Where(x => x.HasBlobName && (string.IsNullOrWhiteSpace(x.StorageKey) || !metadata.TryGetValue(x.StorageKey, out var meta) || !meta.Exists))
            .Select(x => x.VehicleId)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        UploadProbeResult? uploadProbe = null;
        if (options.UploadProbe)
        {
            uploadProbe = await RunUploadProbeAsync(ct);
        }

        ImportRunResult? testImport = null;
        if (options.TestImport)
        {
            var testVehicleId = options.TestVehicleId ?? orphanVehicleIds.FirstOrDefault();
            if (testVehicleId <= 0)
            {
                logger.LogWarning("Nenhum veiculo candidato encontrado para teste real.");
            }
            else
            {
                testImport = await RunSingleVehicleImportAsync(testVehicleId, "StorageAudit-TesteUnico", ct);
            }
        }

        var reimports = new List<ImportRunResult>();
        if (options.ReimportOrphans)
        {
            var reimportIds = orphanVehicleIds;
            if (options.MaxReimportVehicles is > 0)
            {
                reimportIds = reimportIds.Take(options.MaxReimportVehicles.Value).ToList();
            }

            foreach (var vehicleId in reimportIds)
            {
                ct.ThrowIfCancellationRequested();
                reimports.Add(await RunSingleVehicleImportAsync(vehicleId, "StorageAudit-ReimportOrfao", ct));
            }
        }

        var report = new StorageAuditReport(
            DateTimeOffset.UtcNow,
            storageOptions.Value.R2.BucketName ?? string.Empty,
            NormalizeOptionalPrefix(options.Prefix),
            summary,
            latestJobs,
            resolvedRecords
                .Where(x => x.HasBlobName && (string.IsNullOrWhiteSpace(x.StorageKey) || !metadata.TryGetValue(x.StorageKey, out var meta) || !meta.Exists))
                .Take(100)
                .Select(x => new BlobIssueSample(x.MediaId, x.VehicleId, x.LegacyVehicleId, x.BlobName, x.StorageKey, "Objeto ausente no R2"))
                .ToList(),
            resolvedRecords
                .Where(x => x.StorageKey is not null
                    && publicUrls.TryGetValue(x.StorageKey, out var url)
                    && url.StatusCode == (int)HttpStatusCode.NotFound)
                .Take(100)
                .Select(x => new BlobIssueSample(x.MediaId, x.VehicleId, x.LegacyVehicleId, x.BlobName, x.StorageKey, "URL publica retornou 404"))
                .ToList(),
            uploadProbe,
            testImport,
            reimports);

        var reportPath = await WriteReportAsync(report, options.OutputPath, ct);
        LogSummary(report, reportPath);
        return summary.InvalidBlobNameRows == 0 && (testImport is null || testImport.Success) ? 0 : 1;
    }

    private async Task<IReadOnlyList<string>> ListBucketKeysAsync(string? prefix, CancellationToken ct)
    {
        var keys = new List<string>();
        var normalizedPrefix = NormalizeOptionalPrefix(prefix);

        await foreach (var item in r2.ListAsync(normalizedPrefix ?? string.Empty, ct))
        {
            keys.Add(item.Key);
        }

        return keys;
    }

    private async Task<IReadOnlyList<MediaAuditRecord>> LoadMediaRecordsAsync(CancellationToken ct)
        => await db.VeiculoMidias
            .AsNoTracking()
            .Include(x => x.Veiculo)
            .Where(x => x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.VeiculoId)
            .ThenBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .Select(x => new MediaAuditRecord(
                x.Id,
                x.VeiculoId,
                x.Veiculo.IdLegado,
                x.Veiculo.Titulo + " " + x.Veiculo.Modelo,
                x.BlobName,
                x.Url,
                x.Container,
                x.NomeArquivo,
                x.ContentType,
                x.TamanhoBytes))
            .ToListAsync(ct);

    private async Task<IReadOnlyList<JobAuditRecord>> LoadLatestJobsAsync(CancellationToken ct)
        => await db.ImportJobs
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(10)
            .Select(x => new JobAuditRecord(
                x.Id,
                x.Status,
                x.DryRun,
                x.SomenteSemBlobName,
                x.Sobrescrever,
                x.TotalVeiculos,
                x.VeiculosProcessados,
                x.TotalImagens,
                x.ImagensImportadas,
                x.ImagensIgnoradas,
                x.ImagensComErro,
                x.CriadoEm,
                x.IniciadoEm,
                x.FinalizadoEm,
                x.UltimaMensagem))
            .ToListAsync(ct);

    private ResolvedMediaAuditRecord ResolveStorageKey(MediaAuditRecord record)
    {
        var hasBlobName = !string.IsNullOrWhiteSpace(record.BlobName);
        var reference = new StorageImageReference(
            record.Url,
            record.BlobName,
            record.Container,
            record.FileName,
            record.ContentType,
            record.SizeBytes);

        var resolved = StoragePath.TryGetKey(reference, PublicBaseUrls(), out var key);
        return new ResolvedMediaAuditRecord(
            record.MediaId,
            record.VehicleId,
            record.LegacyVehicleId,
            record.VehicleName,
            record.BlobName,
            record.Url,
            hasBlobName,
            resolved ? key : null);
    }

    private async Task<IReadOnlyDictionary<string, MetadataAudit>> ValidateMetadataAsync(
        IReadOnlyList<string> keys,
        CancellationToken ct)
    {
        var results = new ConcurrentDictionary<string, MetadataAudit>(StringComparer.OrdinalIgnoreCase);
        await Parallel.ForEachAsync(keys, new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = auditOptions.Value.SafeMetadataParallelism
        }, async (key, itemCt) =>
        {
            try
            {
                var metadata = await r2.GetMetadataAsync(key, itemCt);
                results[key] = metadata is null
                    ? new MetadataAudit(key, false, null, null, null)
                    : new MetadataAudit(key, true, metadata.ContentType, metadata.SizeBytes, metadata.LastModified);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                results[key] = new MetadataAudit(key, false, null, null, null, ex.Message);
            }
        });

        return results;
    }

    private async Task<IReadOnlyDictionary<string, PublicUrlAudit>> ValidatePublicUrlsAsync(
        IReadOnlyList<string> keys,
        CancellationToken ct)
    {
        var results = new ConcurrentDictionary<string, PublicUrlAudit>(StringComparer.OrdinalIgnoreCase);
        var client = httpClientFactory.CreateClient("storage-audit-public-url");
        await Parallel.ForEachAsync(keys, new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = auditOptions.Value.SafePublicUrlParallelism
        }, async (key, itemCt) =>
        {
            results[key] = await ValidatePublicUrlAsync(client, key, itemCt);
        });

        return results;
    }

    private async Task<PublicUrlAudit> ValidatePublicUrlAsync(HttpClient client, string key, CancellationToken ct)
    {
        var url = r2.GetPublicUrl(key);
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return new PublicUrlAudit(key, url, null, false, null, null, "URL publica invalida.");
        }

        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResponse = await client.SendAsync(head, HttpCompletionOption.ResponseHeadersRead, ct);
            if (headResponse.StatusCode != HttpStatusCode.MethodNotAllowed)
            {
                return new PublicUrlAudit(
                    key,
                    url,
                    (int)headResponse.StatusCode,
                    headResponse.IsSuccessStatusCode,
                    headResponse.Content.Headers.ContentType?.MediaType,
                    headResponse.Content.Headers.ContentLength,
                    null);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PublicUrlAudit(key, url, null, false, null, null, ex.Message);
        }

        try
        {
            using var get = new HttpRequestMessage(HttpMethod.Get, uri);
            using var getResponse = await client.SendAsync(get, HttpCompletionOption.ResponseHeadersRead, ct);
            return new PublicUrlAudit(
                key,
                url,
                (int)getResponse.StatusCode,
                getResponse.IsSuccessStatusCode,
                getResponse.Content.Headers.ContentType?.MediaType,
                getResponse.Content.Headers.ContentLength,
                null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new PublicUrlAudit(key, url, null, false, null, null, ex.Message);
        }
    }

    private StorageAuditSummary BuildSummary(
        IReadOnlyList<MediaAuditRecord> mediaRecords,
        IReadOnlyList<ResolvedMediaAuditRecord> resolvedRecords,
        IReadOnlyList<string> bucketKeys,
        IReadOnlyDictionary<string, MetadataAudit> metadata,
        IReadOnlyDictionary<string, PublicUrlAudit> publicUrls)
    {
        var bucketSet = bucketKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var recordsWithBlob = resolvedRecords.Where(x => x.HasBlobName).ToList();
        var existingRows = recordsWithBlob.Count(x => x.StorageKey is not null
            && metadata.TryGetValue(x.StorageKey, out var meta)
            && meta.Exists);
        var public404Rows = recordsWithBlob.Count(x => x.StorageKey is not null
            && publicUrls.TryGetValue(x.StorageKey, out var url)
            && url.StatusCode == (int)HttpStatusCode.NotFound);
        var publicOkRows = recordsWithBlob.Count(x => x.StorageKey is not null
            && publicUrls.TryGetValue(x.StorageKey, out var url)
            && url.Success);
        var vehiclesWithMissingR2Objects = recordsWithBlob
            .Where(x => string.IsNullOrWhiteSpace(x.StorageKey)
                || !metadata.TryGetValue(x.StorageKey, out var meta)
                || !meta.Exists)
            .Select(x => x.VehicleId)
            .Distinct()
            .Count();
        var vehiclesWithInvalidBlobNames = recordsWithBlob
            .Where(x => string.IsNullOrWhiteSpace(x.StorageKey)
                || !metadata.TryGetValue(x.StorageKey, out var meta)
                || !meta.Exists
                || (auditOptions.Value.ValidatePublicUrls
                    && publicUrls.TryGetValue(x.StorageKey, out var url)
                    && !url.Success))
            .Select(x => x.VehicleId)
            .Distinct()
            .Count();
        var invalidRows = recordsWithBlob.Count(x => string.IsNullOrWhiteSpace(x.StorageKey)
            || !metadata.TryGetValue(x.StorageKey, out var meta)
            || !meta.Exists
            || (auditOptions.Value.ValidatePublicUrls
                && publicUrls.TryGetValue(x.StorageKey, out var url)
                && !url.Success));

        var vehiclesWithBlob = resolvedRecords
            .Where(x => x.HasBlobName)
            .Select(x => x.VehicleId)
            .Distinct()
            .Count();
        var vehiclesWithoutBlob = mediaRecords
            .Select(x => x.VehicleId)
            .Distinct()
            .Count(vehicleId => !resolvedRecords.Any(x => x.VehicleId == vehicleId && x.HasBlobName));
        var vehiclesWith404 = resolvedRecords
            .Where(x => x.StorageKey is not null
                && publicUrls.TryGetValue(x.StorageKey, out var url)
                && url.StatusCode == (int)HttpStatusCode.NotFound)
            .Select(x => x.VehicleId)
            .Distinct()
            .Count();

        return new StorageAuditSummary(
            db.Veiculos.Count(),
            mediaRecords.Select(x => x.VehicleId).Distinct().Count(),
            mediaRecords.Count,
            vehiclesWithBlob,
            vehiclesWithoutBlob,
            recordsWithBlob.Count,
            recordsWithBlob.Select(x => x.BlobName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            resolvedRecords.Count(x => !x.HasBlobName),
            resolvedRecords.Where(x => !string.IsNullOrWhiteSpace(x.StorageKey)).Select(x => x.StorageKey!).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            bucketKeys.Count,
            resolvedRecords.Count(x => x.StorageKey is not null && bucketSet.Contains(x.StorageKey)),
            existingRows,
            vehiclesWithMissingR2Objects,
            invalidRows,
            vehiclesWithInvalidBlobNames,
            recordsWithBlob.Count - invalidRows,
            publicOkRows,
            public404Rows,
            vehiclesWith404);
    }

    private async Task<UploadProbeResult> RunUploadProbeAsync(CancellationToken ct)
    {
        var options = auditOptions.Value;
        var key = string.IsNullOrWhiteSpace(options.ProbeKey)
            ? $"_audit/storage-probe-{DateTime.UtcNow:yyyyMMddHHmmss}.png"
            : StoragePath.NormalizeKey(options.ProbeKey);

        await using var input = new MemoryStream(ProbePngBytes);
        var stored = await r2.SaveAsync(key, input, "image/png", ct);
        var metadata = await r2.GetMetadataAsync(stored.Key, ct);
        var publicUrl = await ValidatePublicUrlAsync(httpClientFactory.CreateClient("storage-audit-public-url"), stored.Key, ct);

        if (!options.KeepProbeObject)
        {
            await r2.DeleteAsync(stored.Key, ct);
        }

        return new UploadProbeResult(
            stored.Key,
            stored.Url,
            metadata is not null,
            publicUrl.Success,
            publicUrl.StatusCode,
            metadata?.SizeBytes,
            !options.KeepProbeObject);
    }

    private async Task<ImportRunResult> RunSingleVehicleImportAsync(int vehicleId, string userName, CancellationToken ct)
    {
        logger.LogWarning("Executando importacao real de um veiculo. VehicleId={VehicleId}", vehicleId);
        using var scope = scopeFactory.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<LegacyImageImportJobManager>();
        var importer = scope.ServiceProvider.GetRequiredService<LegacyVehicleImageImportService>();

        var job = await manager.StartAsync(new LegacyImageImportRequest
        {
            BaseUrl = auditOptions.Value.BaseUrl,
            OnlyWithoutBlobName = false,
            OverwriteExisting = true,
            DryRun = false,
            StartId = vehicleId,
            MaxVehicles = 1
        }, null, userName, ct);

        var workerId = $"{Environment.MachineName}:storage-audit:{Guid.NewGuid():N}";
        await importer.RunAsync(job.Id, workerId, ct);

        var completedJob = await db.ImportJobs.AsNoTracking().FirstAsync(x => x.Id == job.Id, ct);
        var items = await db.ImportJobItems
            .AsNoTracking()
            .Where(x => x.ImportJobId == job.Id)
            .OrderBy(x => x.Ordem)
            .Select(x => new ImportItemResult(
                x.Id,
                x.VeiculoId,
                x.Ordem,
                x.BlobNameDestino,
                x.UrlDestino,
                x.Status,
                x.ContentType,
                x.TamanhoBytes,
                x.Erro))
            .ToListAsync(ct);

        var publicChecks = new List<ImportPublicUrlCheck>();
        foreach (var item in items.Where(x => x.Status == LegacyImageImportItemStatus.Completed))
        {
            var metadata = await r2.GetMetadataAsync(item.BlobName, ct);
            var publicUrl = await ValidatePublicUrlAsync(httpClientFactory.CreateClient("storage-audit-public-url"), item.BlobName, ct);
            publicChecks.Add(new ImportPublicUrlCheck(
                item.ItemId,
                item.BlobName,
                publicUrl.Url,
                metadata is not null,
                publicUrl.Success,
                publicUrl.StatusCode,
                publicUrl.ContentType,
                publicUrl.ContentLength));
        }

        var persistedBlobRows = await db.VeiculoMidias
            .AsNoTracking()
            .CountAsync(x => x.VeiculoId == vehicleId
                && x.Ativo
                && x.Tipo == TipoMidia.Imagem
                && !string.IsNullOrWhiteSpace(x.BlobName), ct);

        var jobFinished = completedJob.Status is LegacyImageImportJobStatus.Completed
            or LegacyImageImportJobStatus.CompletedWithFailures;
        var success = jobFinished
            && items.Any(x => x.Status == LegacyImageImportItemStatus.Completed)
            && publicChecks.Any(x => x.MetadataExists && x.PublicUrlSuccess);

        return new ImportRunResult(
            job.Id,
            vehicleId,
            completedJob.Status,
            completedJob.DryRun,
            completedJob.SomenteSemBlobName,
            completedJob.Sobrescrever,
            items.Count,
            items.Count(x => x.Status == LegacyImageImportItemStatus.Completed),
            items.Count(x => x.Status == LegacyImageImportItemStatus.Ignored),
            items.Count(x => x.Status == LegacyImageImportItemStatus.Failed || x.Status == LegacyImageImportItemStatus.Review),
            persistedBlobRows,
            publicChecks,
            items,
            success,
            completedJob.UltimaMensagem);
    }

    private async Task<string> WriteReportAsync(StorageAuditReport report, string? configuredPath, CancellationToken ct)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "artifacts", $"storage-audit-{DateTime.UtcNow:yyyyMMddHHmmss}.json")
            : configuredPath;

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(report, JsonOptions), ct);
        return fullPath;
    }

    private void LogSummary(StorageAuditReport report, string reportPath)
    {
        var summary = report.Summary;
        logger.LogInformation(
            "Auditoria concluida. VeiculosComBlob={VehiclesWithBlob}; VeiculosSemBlob={VehiclesWithoutBlob}; BlobRowsValidos={Valid}; BlobRowsInvalidos={Invalid}; ObjetosBucket={BucketObjects}; BlobRowsExistentesR2={Existing}; Public404Rows={Public404}; Report={ReportPath}",
            summary.VehiclesWithBlobName,
            summary.VehiclesWithoutBlobName,
            summary.ValidBlobNameRows,
            summary.InvalidBlobNameRows,
            summary.R2ObjectCount,
            summary.BlobNameRowsExistingInR2,
            summary.PublicUrl404Rows,
            reportPath);

        if (report.UploadProbe is not null)
        {
            logger.LogInformation(
                "Probe upload. Key={Key}; Metadata={Metadata}; PublicUrl={PublicUrl}; Status={StatusCode}; Deleted={Deleted}",
                report.UploadProbe.Key,
                report.UploadProbe.MetadataConfirmed,
                report.UploadProbe.PublicUrlSuccess,
                report.UploadProbe.PublicUrlStatusCode,
                report.UploadProbe.DeletedAfterProbe);
        }

        if (report.TestImport is not null)
        {
            logger.LogInformation(
                "Teste real. Job={JobId}; Vehicle={VehicleId}; Status={Status}; Imported={Imported}; PersistedBlobRows={Persisted}; Success={Success}",
                report.TestImport.JobId,
                report.TestImport.VehicleId,
                report.TestImport.Status,
                report.TestImport.ImportedItems,
                report.TestImport.PersistedBlobRows,
                report.TestImport.Success);
        }
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return storageOptions.Value.PublicBaseUrl;
        yield return storageOptions.Value.R2.PublicBaseUrl;
        yield return storageOptions.Value.R2.ServiceUrl;
    }

    private static string? NormalizeOptionalPrefix(string? prefix)
        => string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().Replace('\\', '/').TrimStart('/');
}

public sealed record StorageAuditReport(
    DateTimeOffset GeneratedAt,
    string BucketName,
    string? Prefix,
    StorageAuditSummary Summary,
    IReadOnlyList<JobAuditRecord> LatestJobs,
    IReadOnlyList<BlobIssueSample> MissingObjectSamples,
    IReadOnlyList<BlobIssueSample> PublicUrl404Samples,
    UploadProbeResult? UploadProbe,
    ImportRunResult? TestImport,
    IReadOnlyList<ImportRunResult> Reimports);

public sealed record StorageAuditSummary(
    int TotalVehicles,
    int VehiclesWithActiveImages,
    int ActiveImageRows,
    int VehiclesWithBlobName,
    int VehiclesWithoutBlobName,
    int BlobNameRows,
    int DistinctBlobNames,
    int ImageRowsWithoutBlobName,
    int DistinctResolvedStorageKeys,
    int R2ObjectCount,
    int ResolvedKeysListedInBucket,
    int BlobNameRowsExistingInR2,
    int VehiclesWithMissingR2Objects,
    int InvalidBlobNameRows,
    int VehiclesWithInvalidBlobNames,
    int ValidBlobNameRows,
    int PublicUrlOkRows,
    int PublicUrl404Rows,
    int VehiclesWithPublicUrl404);

public sealed record JobAuditRecord(
    int Id,
    string Status,
    bool DryRun,
    bool OnlyWithoutBlobName,
    bool OverwriteExisting,
    int TotalVehicles,
    int VehiclesProcessed,
    int TotalImages,
    int ImagesImported,
    int ImagesSkipped,
    int ImagesWithError,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    string? LastMessage);

public sealed record BlobIssueSample(
    int MediaId,
    int VehicleId,
    int? LegacyVehicleId,
    string? BlobName,
    string? StorageKey,
    string Message);

public sealed record UploadProbeResult(
    string Key,
    string PublicUrl,
    bool MetadataConfirmed,
    bool PublicUrlSuccess,
    int? PublicUrlStatusCode,
    long? SizeBytes,
    bool DeletedAfterProbe);

public sealed record ImportRunResult(
    int JobId,
    int VehicleId,
    string Status,
    bool DryRun,
    bool OnlyWithoutBlobName,
    bool OverwriteExisting,
    int TotalItems,
    int ImportedItems,
    int IgnoredItems,
    int FailedItems,
    int PersistedBlobRows,
    IReadOnlyList<ImportPublicUrlCheck> PublicUrlChecks,
    IReadOnlyList<ImportItemResult> Items,
    bool Success,
    string? LastMessage);

public sealed record ImportPublicUrlCheck(
    int ItemId,
    string BlobName,
    string PublicUrl,
    bool MetadataExists,
    bool PublicUrlSuccess,
    int? PublicUrlStatusCode,
    string? ContentType,
    long? ContentLength);

public sealed record ImportItemResult(
    int ItemId,
    int VehicleId,
    int Order,
    string BlobName,
    string? PublicUrl,
    string Status,
    string? ContentType,
    long? SizeBytes,
    string? Error);

internal sealed record MediaAuditRecord(
    int MediaId,
    int VehicleId,
    int? LegacyVehicleId,
    string VehicleName,
    string? BlobName,
    string? Url,
    string? Container,
    string? FileName,
    string? ContentType,
    long? SizeBytes);

internal sealed record ResolvedMediaAuditRecord(
    int MediaId,
    int VehicleId,
    int? LegacyVehicleId,
    string VehicleName,
    string? BlobName,
    string? Url,
    bool HasBlobName,
    string? StorageKey);

internal sealed record MetadataAudit(
    string Key,
    bool Exists,
    string? ContentType,
    long? SizeBytes,
    DateTimeOffset? LastModified,
    string? Error = null);

internal sealed record PublicUrlAudit(
    string Key,
    string Url,
    int? StatusCode,
    bool Success,
    string? ContentType,
    long? ContentLength,
    string? Error = null);
