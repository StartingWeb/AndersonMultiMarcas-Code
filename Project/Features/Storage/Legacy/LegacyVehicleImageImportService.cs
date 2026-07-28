using System.Globalization;
using System.Net;
using System.Text.Json;
using Core.Storage;
using Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyVehicleImageImportService(
    ApplicationDbContext db,
    IOptions<StorageOptions> storageOptions,
    IOptions<LegacyImageImportOptions> importOptions,
    IHttpClientFactory httpClientFactory,
    LegacyVehicleJsonLdParser jsonLdParser,
    IServiceScopeFactory scopeFactory,
    LegacyImageImportReportService reportService,
    ILogger<LegacyVehicleImageImportService> logger)
{
    public const string HttpClientName = "legacy-image-import";
    private const int MaxRedirects = 3;

    public async Task RunAsync(int jobId, string workerId, CancellationToken ct)
    {
        try
        {
            var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == jobId, ct);
            if (job is null || LegacyImageImportJobStatus.IsTerminal(job.Status))
            {
                return;
            }

            if (job.Status == LegacyImageImportJobStatus.Cancelling)
            {
                job.MarkCancelled();
                AddLog(job.Id, null, null, null, "Sistema", "Cancelado", "Importacao cancelada antes da execucao.");
                AddHistory(job.Id, "Cancelado", job.UsuarioId, job.UsuarioNome, null, job.FinalizadoEm - job.IniciadoEm, job.Status, "Importacao cancelada antes da execucao.");
                await db.SaveChangesAsync(ct);
                return;
            }

            if (!await AcquireJobAsync(job, workerId, ct))
            {
                return;
            }

            if (!job.DryRun && !storageOptions.Value.UseR2ForWrites)
            {
                AddLog(job.Id, null, null, null, "Configuracao", "Erro", "Storage:Provider precisa ser R2 para executar importacao real.");
                job.MarkFailed("Configure Storage:Provider=R2 antes de executar a importacao real.");
                await db.SaveChangesAsync(ct);
                return;
            }

            if (!job.PreparacaoConcluida)
            {
                await PrepareItemsAsync(job, workerId, ct);
            }

            await ProcessPendingItemsAsync(job.Id, workerId, ct);
            await CompleteJobAsync(job.Id, ct);
        }
        catch (OperationCanceledException)
        {
            await PersistCancellationIfRequestedAsync(jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na importacao legado {JobId}.", jobId);
            await PersistFailureAsync(jobId, ex.Message);
        }
    }

    private async Task<bool> AcquireJobAsync(ImportJob job, string workerId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (job.Status == LegacyImageImportJobStatus.Running
            && job.LockExpiraEm.HasValue
            && job.LockExpiraEm.Value > now
            && !string.Equals(job.LockId, workerId, StringComparison.Ordinal))
        {
            AddLog(job.Id, null, null, null, "Sistema", "Ignorado", "Job ja esta bloqueado por outro worker.");
            await db.SaveChangesAsync(ct);
            return false;
        }

        job.MarkRunning(workerId, now.Add(importOptions.Value.SafeLockTimeout));
        AddLog(job.Id, null, null, null, "Sistema", "Execucao", "Worker assumiu o job.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task PrepareItemsAsync(ImportJob job, string workerId, CancellationToken ct)
    {
        var options = importOptions.Value;
        var baseUri = BuildBaseUri(job.UrlBase, options.AllowedHosts);

        var query = db.Veiculos
            .Include(x => x.Midias)
            .OrderBy(x => x.Id)
            .AsQueryable();

        if (job.IdInicial.HasValue)
        {
            query = query.Where(x => x.Id >= job.IdInicial.Value);
        }

        if (job.QuantidadeMaxima.HasValue)
        {
            query = query.Take(job.QuantidadeMaxima.Value);
        }

        var vehicles = await query.ToListAsync(ct);
        job.SetTotals(vehicles.Count, await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id, ct));
        AddLog(job.Id, null, null, null, "Preparacao", "Inicio", $"Veiculos selecionados: {vehicles.Count}.");
        await db.SaveChangesAsync(ct);

        foreach (var vehicle in vehicles)
        {
            await ThrowIfCancellationRequestedAsync(job.Id, ct);
            await RefreshJobLockAsync(job.Id, workerId, ct);

            job.SetCurrentVehicle(vehicle.Id);
            await db.SaveChangesAsync(ct);

            if (await db.ImportJobItems.AnyAsync(x => x.ImportJobId == job.Id && x.VeiculoId == vehicle.Id, ct))
            {
                AddLog(job.Id, null, vehicle.Id, null, "Preparacao", "Ignorado", "Veiculo ja possui itens persistidos neste job.");
                await db.SaveChangesAsync(ct);
                continue;
            }

            await PrepareVehicleItemsAsync(job, vehicle, baseUri, ct);
        }

        var totalImages = await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id, ct);
        job.MarkPreparationCompleted(totalImages);
        job.SetCurrentVehicle(null);
        AddLog(job.Id, null, null, null, "Preparacao", "Sucesso", $"Preparacao concluida. Itens persistidos: {totalImages}.");
        await db.SaveChangesAsync(ct);
    }

    private async Task PrepareVehicleItemsAsync(ImportJob job, Domain.Entities.Veiculo vehicle, Uri baseUri, CancellationToken ct)
    {
        var activeImages = vehicle.Midias
            .Where(x => x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToList();

        if (job.SomenteSemBlobName && activeImages.Any(x => !string.IsNullOrWhiteSpace(x.BlobName)))
        {
            AddLog(job.Id, null, vehicle.Id, null, "Veiculo", "Ignorado", "Veiculo ignorado porque ja possui BlobName.");
            await db.SaveChangesAsync(ct);
            return;
        }

        if (!job.Sobrescrever && activeImages.Any(x => IsLegacyImported(vehicle.Id, x.BlobName)))
        {
            AddLog(job.Id, null, vehicle.Id, null, "Veiculo", "Ignorado", "Veiculo ja possui imagens importadas do legado.");
            await db.SaveChangesAsync(ct);
            return;
        }

        var pageUri = new Uri(baseUri, $"/veiculo/{vehicle.Id}/");
        AddLog(job.Id, null, vehicle.Id, null, "Pagina", "Iniciado", $"GET {pageUri}");
        await db.SaveChangesAsync(ct);

        LegacyPageResult pageResult;
        try
        {
            pageResult = await FetchLegacyPageAsync(pageUri, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AddLog(job.Id, null, vehicle.Id, null, "Pagina", "Erro", ex.Message);
            await db.SaveChangesAsync(ct);
            return;
        }

        if (pageResult.StatusCode == HttpStatusCode.NotFound)
        {
            AddLog(job.Id, null, vehicle.Id, null, "Pagina", "Erro", "Pagina legado retornou 404.");
            await db.SaveChangesAsync(ct);
            return;
        }

        if (pageResult.StatusCode != HttpStatusCode.OK || string.IsNullOrWhiteSpace(pageResult.Html))
        {
            AddLog(job.Id, null, vehicle.Id, null, "Pagina", "Erro", $"Pagina legado retornou {(int)pageResult.StatusCode}.");
            await db.SaveChangesAsync(ct);
            return;
        }

        IReadOnlyList<string> imageUrls;
        try
        {
            imageUrls = jsonLdParser.ExtractVehicleImageUrls(pageResult.Html, pageUri);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            AddLog(job.Id, null, vehicle.Id, null, "JSON-LD", "Erro", $"JSON-LD invalido: {ex.Message}");
            await db.SaveChangesAsync(ct);
            return;
        }

        if (imageUrls.Count == 0)
        {
            AddLog(job.Id, null, vehicle.Id, null, "JSON-LD", "Erro", "Nenhuma imagem encontrada no JSON-LD Vehicle.");
            await db.SaveChangesAsync(ct);
            return;
        }

        AddLog(job.Id, null, vehicle.Id, null, "JSON-LD", "Sucesso", $"{imageUrls.Count} imagem(ns) encontrada(s).");

        foreach (var item in BuildWorkItems(vehicle.Id, imageUrls))
        {
            if (await db.ImportJobItems.AnyAsync(x => x.ImportJobId == job.Id
                    && (x.UrlLegada == item.SourceUrl || x.BlobNameDestino == item.StorageKey), ct))
            {
                AddLog(job.Id, null, vehicle.Id, item.Index + 1, "Preparacao", "Ignorado", "Imagem ja existia neste job.", item.SourceUrl);
                continue;
            }

            var association = ResolveAssociation(activeImages, item);
            var jobItem = new ImportJobItem(
                job.Id,
                vehicle.Id,
                association.MediaId,
                item.Index,
                item.Index == 0,
                item.SourceUrl,
                item.FileName,
                item.StorageKey,
                importOptions.Value.SafeMaxAttempts);

            db.ImportJobItems.Add(jobItem);

            if (job.DryRun)
            {
                jobItem.MarkIgnored($"Dry run: imagem seria importada para {item.StorageKey}.");
                AddLog(job.Id, null, vehicle.Id, item.Index + 1, "Dry Run", "Ignorado", $"Imagem seria importada para {item.StorageKey}.", item.SourceUrl);
                continue;
            }

            if (!association.IsDeterministic)
            {
                jobItem.RequestReview(association.Message);
                AddLog(job.Id, null, vehicle.Id, item.Index + 1, "Associacao", "PendenteRevisao", association.Message, item.SourceUrl);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ProcessPendingItemsAsync(int jobId, string workerId, CancellationToken ct)
    {
        var uploadSemaphore = new SemaphoreSlim(importOptions.Value.SafeMaxParallelUploads);
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = importOptions.Value.SafeMaxParallelDownloads
        };

        while (true)
        {
            await ThrowIfCancellationRequestedAsync(jobId, ct);
            await RefreshJobLockAsync(jobId, workerId, ct);

            var pendingIds = await db.ImportJobItems
                .AsNoTracking()
                .Where(x => x.ImportJobId == jobId
                    && (x.Status == LegacyImageImportItemStatus.Pending
                        || (x.Status == LegacyImageImportItemStatus.Failed && x.Tentativas < x.MaxTentativas)))
                .OrderBy(x => x.VeiculoId)
                .ThenBy(x => x.Ordem)
                .Select(x => x.Id)
                .Take(200)
                .ToListAsync(ct);

            if (pendingIds.Count == 0)
            {
                break;
            }

            await Parallel.ForEachAsync(pendingIds, parallelOptions, async (itemId, itemCt) =>
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<LegacyVehicleImageImportItemProcessor>();
                await processor.ProcessAsync(jobId, itemId, workerId, uploadSemaphore, itemCt);
            });

            var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
            await UpdateProgressAsync(job, ct);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task CompleteJobAsync(int jobId, CancellationToken ct)
    {
        var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
        if (job.Status == LegacyImageImportJobStatus.Cancelling)
        {
            job.MarkCancelled();
            AddLog(job.Id, null, null, null, "Sistema", "Cancelado", "Importacao cancelada pelo usuario.");
            await db.SaveChangesAsync(ct);
            return;
        }

        await UpdateProgressAsync(job, ct);
        job.MarkCompleted();
        AddLog(job.Id, null, null, null, "Fim", job.Status, "Importacao finalizada.");
        AddHistory(job.Id, "Finalizado", job.UsuarioId, job.UsuarioNome, job.TotalImagens, job.FinalizadoEm - job.IniciadoEm, job.Status, "Importacao finalizada.");
        await db.SaveChangesAsync(ct);
        await reportService.StoreConsolidatedReportAsync(job.Id, ct);
    }

    private async Task PersistCancellationIfRequestedAsync(int jobId)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == jobId, CancellationToken.None);
        if (job is null || job.Status != LegacyImageImportJobStatus.Cancelling)
        {
            return;
        }

        var runningItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == jobId && x.Status == LegacyImageImportItemStatus.Running)
            .ToListAsync(CancellationToken.None);
        foreach (var item in runningItems)
        {
            item.MarkPending("Cancelado antes da conclusao do item.");
        }

        job.MarkCancelled();
        AddLog(job.Id, null, null, null, "Sistema", "Cancelado", "Importacao cancelada pelo usuario.");
        AddHistory(job.Id, "Cancelado", job.UsuarioId, job.UsuarioNome, null, job.FinalizadoEm - job.IniciadoEm, job.Status, "Importacao cancelada pelo usuario.");
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task PersistFailureAsync(int jobId, string message)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(x => x.Id == jobId, CancellationToken.None);
        if (job is null || LegacyImageImportJobStatus.IsTerminal(job.Status))
        {
            return;
        }

        job.MarkFailed(message);
        AddLog(job.Id, null, null, null, "Sistema", "Erro", message);
        AddHistory(job.Id, "Falha", job.UsuarioId, job.UsuarioNome, null, job.FinalizadoEm - job.IniciadoEm, job.Status, message);
        await db.SaveChangesAsync(CancellationToken.None);
    }

    private async Task UpdateProgressAsync(ImportJob job, CancellationToken ct)
    {
        var imported = await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id && x.Status == LegacyImageImportItemStatus.Completed, ct);
        var skipped = await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id && x.Status == LegacyImageImportItemStatus.Ignored, ct);
        var failed = await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id
            && (x.Status == LegacyImageImportItemStatus.Failed || x.Status == LegacyImageImportItemStatus.Review), ct);

        var vehiclesWithItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == job.Id)
            .Select(x => x.VeiculoId)
            .Distinct()
            .ToListAsync(ct);
        var vehiclesWithOpenItems = await db.ImportJobItems
            .Where(x => x.ImportJobId == job.Id && !LegacyImageImportItemStatus.Terminal.Contains(x.Status))
            .Select(x => x.VeiculoId)
            .Distinct()
            .ToListAsync(ct);

        job.UpdateProgress(vehiclesWithItems.Except(vehiclesWithOpenItems).Count(), imported, skipped, failed);
        job.SetTotals(job.TotalVeiculos, await db.ImportJobItems.CountAsync(x => x.ImportJobId == job.Id, ct));
    }

    private async Task ThrowIfCancellationRequestedAsync(int jobId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var status = await db.ImportJobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.Status)
            .FirstAsync(ct);

        if (status == LegacyImageImportJobStatus.Cancelling)
        {
            throw new OperationCanceledException("Cancelamento solicitado pelo usuario.", ct);
        }
    }

    private async Task RefreshJobLockAsync(int jobId, string workerId, CancellationToken ct)
    {
        var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
        if (job.Status == LegacyImageImportJobStatus.Running && string.Equals(job.LockId, workerId, StringComparison.Ordinal))
        {
            job.RefreshLock(DateTime.UtcNow.Add(importOptions.Value.SafeLockTimeout));
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<LegacyPageResult> FetchLegacyPageAsync(Uri pageUri, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(importOptions.Value.SafePageTimeout);

        HttpResponseMessage response;
        try
        {
            response = await SendWithRetryAsync(pageUri, HttpCompletionOption.ResponseContentRead, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Timeout ao baixar pagina do legado.");
        }

        using (response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new LegacyPageResult(response.StatusCode, null);
            }

            var html = await response.Content.ReadAsStringAsync(timeout.Token);
            return new LegacyPageResult(response.StatusCode, html);
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(Uri uri, HttpCompletionOption completionOption, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= importOptions.Value.SafeMaxAttempts; attempt++)
        {
            try
            {
                var response = await SendWithSafeRedirectsAsync(uri, completionOption, ct);
                if ((int)response.StatusCode >= 500 && attempt < importOptions.Value.SafeMaxAttempts)
                {
                    response.Dispose();
                    throw new HttpRequestException($"Servidor remoto retornou {(int)response.StatusCode}.");
                }

                return response;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch when (attempt < importOptions.Value.SafeMaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(350 * Math.Pow(2, attempt - 1)), ct);
            }
        }

        return await SendWithSafeRedirectsAsync(uri, completionOption, ct);
    }

    private async Task<HttpResponseMessage> SendWithSafeRedirectsAsync(Uri uri, HttpCompletionOption completionOption, CancellationToken ct)
    {
        var current = uri;
        var client = httpClientFactory.CreateClient(HttpClientName);

        for (var redirect = 0; redirect <= MaxRedirects; redirect++)
        {
            await LegacySourceUrlGuard.ValidateAsync(current, importOptions.Value.AllowedHosts, ct);
            var response = await client.GetAsync(current, completionOption, ct);
            if (!IsRedirect(response.StatusCode))
            {
                return response;
            }

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null)
            {
                throw new InvalidOperationException("Redirect sem cabecalho Location.");
            }

            current = location.IsAbsoluteUri ? location : new Uri(current, location);
        }

        throw new InvalidOperationException("Limite de redirects excedido.");
    }

    private void AddLog(int jobId, int? itemId, int? vehicleId, int? imageIndex, string stage, string status, string message, string? imageUrl = null)
        => db.ImportJobLogs.Add(new ImportJobLog(jobId, itemId, vehicleId, imageIndex, imageUrl, stage, status, message));

    private void AddHistory(
        int jobId,
        string type,
        string? userId,
        string? userName,
        int? quantity,
        TimeSpan? duration,
        string? result,
        string? message)
        => db.ImportJobHistory.Add(new ImportJobHistory(jobId, type, userId, userName, quantity, duration, result, message));

    private static IReadOnlyList<LegacyImageWorkItem> BuildWorkItems(int vehicleId, IReadOnlyList<string> imageUrls)
    {
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return imageUrls
            .Select((url, index) =>
            {
                var fileName = BuildFileName(url, index, usedNames);
                var key = StoragePath.Combine("anderson-multimarcas", "veiculos", vehicleId.ToString(CultureInfo.InvariantCulture), fileName);
                return new LegacyImageWorkItem(index, url, fileName, key);
            })
            .ToList();
    }

    private static string BuildFileName(string imageUrl, int index, ISet<string> usedNames)
    {
        var uri = new Uri(imageUrl);
        var fileName = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"imagem-{index + 1}.jpg";
        }

        foreach (var invalid in Path.GetInvalidFileNameChars().Concat(['/', '\\']))
        {
            fileName = fileName.Replace(invalid, '-');
        }

        fileName = fileName.Trim();
        if (string.IsNullOrWhiteSpace(Path.GetExtension(fileName)))
        {
            fileName += ".jpg";
        }

        var unique = fileName;
        var suffix = 2;
        while (!usedNames.Add(unique))
        {
            unique = $"{Path.GetFileNameWithoutExtension(fileName)}-{suffix}{Path.GetExtension(fileName)}";
            suffix++;
        }

        return unique;
    }

    private static AssociationResult ResolveAssociation(IReadOnlyList<VeiculoMidia> activeImages, LegacyImageWorkItem item)
    {
        if (activeImages.Count == 0)
        {
            return AssociationResult.New("Veiculo sem midias ativas; nova midia sera criada.");
        }

        var byBlob = activeImages
            .Where(x => string.Equals(NormalizeStorageKey(x.BlobName), item.StorageKey, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        if (byBlob.Count == 1)
        {
            return AssociationResult.Match(byBlob[0], "Associado por BlobName.");
        }

        var byUrl = activeImages
            .Where(x => SameUrl(x.Url, item.SourceUrl))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        if (byUrl.Count == 1)
        {
            return AssociationResult.Match(byUrl[0], "Associado por URL legada.");
        }

        var sourceFileName = NormalizeFileNameFromUrl(item.SourceUrl);
        var byFileName = activeImages
            .Where(x => string.Equals(NormalizeFileName(x.NomeArquivo), sourceFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeFileNameFromUrl(x.Url), sourceFileName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeFileNameFromUrl(x.BlobName), sourceFileName, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Id)
            .Distinct()
            .ToList();
        if (byFileName.Count == 1)
        {
            return AssociationResult.Match(byFileName[0], "Associado por nome de arquivo.");
        }

        return AssociationResult.Review("Associacao deterministica nao encontrada; item aguardando revisao.");
    }

    private static Uri BuildBaseUri(string baseUrl, string[] allowedHosts)
    {
        if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !LegacySourceUrlGuard.IsAllowedHost(uri.Host, allowedHosts))
        {
            throw new InvalidOperationException("URL base do site legado deve ser HTTPS e pertencer ao dominio permitido.");
        }

        return uri;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static bool IsLegacyImported(int vehicleId, string? blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return false;
        }

        var prefix = StoragePath.Combine("anderson-multimarcas", "veiculos", vehicleId.ToString(CultureInfo.InvariantCulture)) + "/";
        return blobName.Trim().Replace('\\', '/').StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SameUrl(string? first, string? second)
    {
        static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Split('?', '#')[0].Trim().TrimEnd('/').ToUpperInvariant();
        }

        return string.Equals(Normalize(first), Normalize(second), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeStorageKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace('\\', '/');

    private static string NormalizeFileNameFromUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            return NormalizeFileName(Uri.UnescapeDataString(Path.GetFileName(absolute.AbsolutePath)));
        }

        return NormalizeFileName(Path.GetFileName(value.Replace('\\', '/')));
    }

    private static string NormalizeFileName(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();

    private sealed record LegacyPageResult(HttpStatusCode StatusCode, string? Html);

    private sealed record AssociationResult(int? MediaId, bool IsDeterministic, string Message)
    {
        public static AssociationResult Match(int mediaId, string message) => new(mediaId, true, message);
        public static AssociationResult New(string message) => new(null, true, message);
        public static AssociationResult Review(string message) => new(null, false, message);
    }
}
