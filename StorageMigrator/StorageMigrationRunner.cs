using Core.Storage;
using Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace StorageMigrator;

public sealed class StorageMigrationRunner(
    ApplicationDbContext db,
    R2StorageService r2,
    IOptions<StorageOptions> storageOptions,
    IOptions<StorageMigrationOptions> migrationOptions,
    ILogger<StorageMigrationRunner> logger)
{
    public async Task<int> RunAsync(CancellationToken ct)
    {
        var options = migrationOptions.Value;
        var stats = new MigrationStats();
        var webRootPath = ResolveWebRootPath(options.WebRootPath);

        if (!r2.IsConfigured)
        {
            logger.LogError("Cloudflare R2 nao esta configurado. Informe Storage:R2:AccountId, AccessKeyId, SecretAccessKey e BucketName.");
            return 2;
        }

        logger.LogInformation(
            "Migrador de storage iniciado. Execute={Execute}; WebRootPath={WebRootPath}; BatchSize={BatchSize}; StartId={StartId}; Limit={Limit}",
            options.Execute,
            webRootPath,
            options.BatchSize,
            options.StartId,
            options.Limit);

        if (!options.Execute)
        {
            logger.LogWarning("Modo dry-run ativo. Nenhum upload e nenhuma atualizacao no banco serao executados.");
        }

        var batchSize = Math.Clamp(options.BatchSize, 1, 500);
        var lastId = Math.Max(0, options.StartId.GetValueOrDefault(1) - 1);
        var remaining = options.Limit;

        while (!ct.IsCancellationRequested)
        {
            var take = remaining.HasValue ? Math.Min(batchSize, remaining.Value) : batchSize;
            if (take <= 0)
            {
                break;
            }

            var batch = await db.VeiculoMidias
                .Where(x => x.Id > lastId && x.Ativo && x.Tipo == TipoMidia.Imagem)
                .OrderBy(x => x.Id)
                .Take(take)
                .ToListAsync(ct);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var midia in batch)
            {
                lastId = midia.Id;
                await ProcessMediaAsync(midia, webRootPath, options.Execute, stats, ct);
            }

            if (remaining.HasValue)
            {
                remaining -= batch.Count;
            }
        }

        logger.LogInformation(
            "Migrador finalizado. Lidos={Scanned}; Migrados={Migrated}; JaMigrados={AlreadyMigrated}; DryRunPendentes={DryRunPending}; Ausentes={MissingFiles}; Invalidos={InvalidRecords}; Falhas={Failed}",
            stats.Scanned,
            stats.Migrated,
            stats.AlreadyMigrated,
            stats.DryRunPending,
            stats.MissingFiles,
            stats.InvalidRecords,
            stats.Failed);

        return stats.Failed > 0 || stats.MissingFiles > 0 || stats.InvalidRecords > 0 ? 1 : 0;
    }

    private async Task ProcessMediaAsync(
        VeiculoMidia midia,
        string webRootPath,
        bool execute,
        MigrationStats stats,
        CancellationToken ct)
    {
        stats.Scanned++;

        if (TryGetAlreadyMigratedKey(midia, out var migratedKey)
            && await ValidateRemoteObjectAsync(migratedKey, midia.TamanhoBytes, ct))
        {
            stats.AlreadyMigrated++;
            logger.LogInformation("Midia {MidiaId} ja esta validada no R2 em {StorageKey}.", midia.Id, migratedKey);
            return;
        }

        if (!StoragePath.TryGetKeyFromSource(midia.Url, PublicBaseUrls(), out var key) || !StoragePath.IsVehicleKey(key))
        {
            stats.InvalidRecords++;
            logger.LogWarning("Midia {MidiaId} ignorada: URL invalida para veiculo. Url={Url}", midia.Id, midia.Url);
            return;
        }

        var fullPath = ResolvePhysicalPath(webRootPath, key);
        if (!File.Exists(fullPath))
        {
            stats.MissingFiles++;
            logger.LogWarning("Midia {MidiaId} sem arquivo fisico correspondente. Esperado={FullPath}", midia.Id, fullPath);
            return;
        }

        var fileInfo = new FileInfo(fullPath);
        var contentType = GetContentType(key);

        if (!execute)
        {
            stats.DryRunPending++;
            logger.LogInformation("Dry-run: midia {MidiaId} seria enviada para R2. Key={StorageKey}; Bytes={Bytes}", midia.Id, key, fileInfo.Length);
            return;
        }

        try
        {
            await using var input = File.OpenRead(fullPath);
            var stored = await r2.SaveAsync(key, input, contentType, ct);
            var metadata = await r2.GetMetadataAsync(stored.Key, ct);

            if (metadata is null)
            {
                stats.Failed++;
                logger.LogError("Midia {MidiaId}: upload para R2 nao foi confirmado via HEAD. Key={StorageKey}", midia.Id, stored.Key);
                return;
            }

            if (metadata.SizeBytes.HasValue && metadata.SizeBytes.Value != fileInfo.Length)
            {
                stats.Failed++;
                logger.LogError(
                    "Midia {MidiaId}: tamanho divergente apos upload. Local={LocalBytes}; R2={RemoteBytes}; Key={StorageKey}",
                    midia.Id,
                    fileInfo.Length,
                    metadata.SizeBytes.Value,
                    stored.Key);
                return;
            }

            midia.UpdateStorage(stored.Key, stored.Container, contentType, fileInfo.Length);
            await db.SaveChangesAsync(ct);

            stats.Migrated++;
            logger.LogInformation("Midia {MidiaId} migrada e atualizada no banco. Key={StorageKey}", midia.Id, stored.Key);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stats.Failed++;
            logger.LogError(ex, "Midia {MidiaId}: falha durante migracao para R2.", midia.Id);
        }
    }

    private bool TryGetAlreadyMigratedKey(VeiculoMidia midia, out string key)
    {
        key = string.Empty;
        var bucketName = storageOptions.Value.R2.BucketName;
        if (string.IsNullOrWhiteSpace(bucketName)
            || string.IsNullOrWhiteSpace(midia.Container)
            || !string.Equals(midia.Container.Trim(), bucketName.Trim(), StringComparison.Ordinal))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(midia.BlobName)
            && StoragePath.IsVehicleKey(midia.BlobName)
            && StoragePath.TryGetKey(new StorageImageReference(midia.Url, midia.BlobName, midia.Container, midia.NomeArquivo, midia.ContentType, midia.TamanhoBytes), PublicBaseUrls(), out key);
    }

    private async Task<bool> ValidateRemoteObjectAsync(string key, long? expectedSizeBytes, CancellationToken ct)
    {
        var metadata = await r2.GetMetadataAsync(key, ct);
        if (metadata is null)
        {
            return false;
        }

        return !expectedSizeBytes.HasValue
            || !metadata.SizeBytes.HasValue
            || expectedSizeBytes.Value == metadata.SizeBytes.Value;
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return storageOptions.Value.PublicBaseUrl;
        yield return storageOptions.Value.R2.PublicBaseUrl;
        yield return storageOptions.Value.R2.ServiceUrl;
    }

    private static string ResolveWebRootPath(string? configuredWebRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredWebRootPath))
        {
            return Path.GetFullPath(configuredWebRootPath);
        }

        var current = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(current, "wwwroot"),
            Path.Combine(current, "Project", "wwwroot"),
            Path.Combine(current, "..", "Project", "wwwroot")
        };

        var found = candidates.Select(Path.GetFullPath).FirstOrDefault(Directory.Exists);
        return found ?? throw new InvalidOperationException("Configure StorageMigration:WebRootPath apontando para o wwwroot de producao.");
    }

    private static string ResolvePhysicalPath(string webRootPath, string key)
    {
        var root = Path.GetFullPath(webRootPath);
        var relative = StoragePath.NormalizeKey(key).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Caminho fisico fora do webroot: {fullPath}");
        }

        return fullPath;
    }

    private static string GetContentType(string key)
    {
        var extension = Path.GetExtension(key);
        if (extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return "image/jpeg";
        }

        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return "image/png";
        }

        if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            return "image/webp";
        }

        return "application/octet-stream";
    }

    private sealed class MigrationStats
    {
        public int Scanned { get; set; }
        public int Migrated { get; set; }
        public int AlreadyMigrated { get; set; }
        public int DryRunPending { get; set; }
        public int MissingFiles { get; set; }
        public int InvalidRecords { get; set; }
        public int Failed { get; set; }
    }
}
