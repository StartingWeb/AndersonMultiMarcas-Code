using System.Net;
using Core.Storage;
using Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Project.Features.Storage.Legacy;

public sealed class LegacyVehicleImageImportItemProcessor(
    ApplicationDbContext db,
    IStorageService storage,
    IOptions<LegacyImageImportOptions> importOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<LegacyVehicleImageImportItemProcessor> logger)
{
    private const int MaxRedirects = 3;

    public async Task ProcessAsync(int jobId, int itemId, string workerId, SemaphoreSlim uploadSemaphore, CancellationToken ct)
    {
        var item = await db.ImportJobItems.FirstOrDefaultAsync(x => x.Id == itemId && x.ImportJobId == jobId, ct);
        if (item is null || item.IsTerminal)
        {
            return;
        }

        var job = await db.ImportJobs.FirstAsync(x => x.Id == jobId, ct);
        if (job.Status == LegacyImageImportJobStatus.Cancelling)
        {
            throw new OperationCanceledException("Cancelamento solicitado pelo usuario.", ct);
        }

        if (!await AcquireItemAsync(item, workerId, ct))
        {
            return;
        }

        try
        {
            if (job.DryRun)
            {
                item.MarkIgnored($"Dry run: imagem seria importada para {item.BlobNameDestino}.");
                AddLog(item, "Dry Run", "Ignorado", $"Imagem seria importada para {item.BlobNameDestino}.");
                await db.SaveChangesAsync(ct);
                return;
            }

            if (await ShouldIgnoreExistingMediaAsync(job, item, ct))
            {
                await db.SaveChangesAsync(ct);
                return;
            }

            var uploaded = await DownloadUploadAndValidateAsync(item, uploadSemaphore, ct);
            await ApplyDatabaseUpdateAsync(job, item.Id, uploaded, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            item.MarkPending("Cancelado antes da conclusao do item.");
            AddLog(item, "Imagem", "Cancelado", "Item devolvido para pendente por cancelamento.");
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Timeout ao importar item {ItemId} do job {JobId}.", item.Id, jobId);
            item.MarkFailed("Timeout ao baixar ou enviar a imagem.");
            AddLog(item, "Imagem", "Erro", "Timeout ao baixar ou enviar a imagem.");
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao importar item {ItemId} do job {JobId}.", item.Id, jobId);
            item.MarkFailed(ex.Message);
            AddLog(item, "Imagem", "Erro", ex.Message);
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> AcquireItemAsync(ImportJobItem item, string workerId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (item.Status == LegacyImageImportItemStatus.Running
            && item.LockExpiraEm.HasValue
            && item.LockExpiraEm.Value > now
            && !string.Equals(item.LockId, workerId, StringComparison.Ordinal))
        {
            return false;
        }

        if (item.Tentativas >= item.MaxTentativas && item.Status == LegacyImageImportItemStatus.Failed)
        {
            return false;
        }

        item.MarkRunning(workerId, now.Add(importOptions.Value.SafeLockTimeout));
        item.IncrementAttempt();
        AddLog(item, "Imagem", "Iniciado", $"Tentativa {item.Tentativas}/{item.MaxTentativas}.");
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> ShouldIgnoreExistingMediaAsync(ImportJob job, ImportJobItem item, CancellationToken ct)
    {
        if (job.Sobrescrever || !item.VeiculoMidiaId.HasValue)
        {
            return false;
        }

        var media = await db.VeiculoMidias.FirstOrDefaultAsync(x => x.Id == item.VeiculoMidiaId.Value, ct);
        if (media is null || string.IsNullOrWhiteSpace(media.BlobName))
        {
            return false;
        }

        if (string.Equals(media.BlobName.Trim().Replace('\\', '/'), item.BlobNameDestino, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        item.MarkIgnored("Registro ja possui BlobName e sobrescrita esta desativada.");
        AddLog(item, "Banco", "Ignorado", "Registro ja possui BlobName e sobrescrita esta desativada.");
        return true;
    }

    private async Task<UploadedImage> DownloadUploadAndValidateAsync(ImportJobItem item, SemaphoreSlim uploadSemaphore, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(importOptions.Value.SafeDownloadTimeout);

        var sourceUri = new Uri(item.UrlLegada);
        AddLog(item, "Download", "Iniciado", "Baixando imagem do legado.");
        await db.SaveChangesAsync(ct);

        var response = await SendWithRetryAsync(sourceUri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        using (response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Download retornou {(int)response.StatusCode}.");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Content-Type invalido: {contentType ?? "(vazio)"}.");
            }

            var contentLength = response.Content.Headers.ContentLength;
            if (contentLength is <= 0)
            {
                throw new InvalidOperationException("Imagem baixada com tamanho zero.");
            }

            await using var source = await response.Content.ReadAsStreamAsync(timeout.Token);
            await using var counting = new CountingReadStream(source);

            await uploadSemaphore.WaitAsync(timeout.Token);
            try
            {
                AddLog(item, "Upload", "Iniciado", $"Enviando para {item.BlobNameDestino}.");
                await db.SaveChangesAsync(ct);

                var stored = await storage.SaveAsync(item.BlobNameDestino, counting, contentType, timeout.Token);
                var bytesRead = counting.BytesRead;
                if (bytesRead <= 0)
                {
                    throw new InvalidOperationException("Stream da imagem nao produziu bytes.");
                }

                if (contentLength.HasValue && contentLength.Value != bytesRead)
                {
                    throw new InvalidOperationException($"Tamanho divergente no download. Header={contentLength.Value}; Stream={bytesRead}.");
                }

                AddLog(item, "Download", "Sucesso", $"{bytesRead} bytes baixados.");

                var metadata = await storage.GetMetadataAsync(stored.Key, timeout.Token)
                    ?? throw new InvalidOperationException("Upload nao confirmado via HEAD/metadados do storage.");
                ValidateMetadata(stored, metadata, bytesRead, contentType);

                AddLog(item, "Upload", "Sucesso", $"Upload confirmado em {stored.Key}.");
                await db.SaveChangesAsync(ct);

                return new UploadedImage(stored, contentType, bytesRead);
            }
            finally
            {
                uploadSemaphore.Release();
            }
        }
    }

    private async Task ApplyDatabaseUpdateAsync(ImportJob job, int itemId, UploadedImage uploaded, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var item = await db.ImportJobItems.FirstAsync(x => x.Id == itemId, ct);
        var activeImages = await db.VeiculoMidias
            .Where(x => x.VeiculoId == item.VeiculoId && x.Ativo && x.Tipo == TipoMidia.Imagem)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        var target = item.VeiculoMidiaId.HasValue
            ? activeImages.FirstOrDefault(x => x.Id == item.VeiculoMidiaId.Value)
            : null;

        var oldBlobName = target?.BlobName;
        if (target is null)
        {
            target = new VeiculoMidia(item.VeiculoId, item.NomeArquivoDestino, uploaded.Stored.Url, TipoMidia.Imagem, item.Ordem);
            db.VeiculoMidias.Add(target);
            activeImages.Add(target);
        }
        else
        {
            target.UpdateUrl(uploaded.Stored.Url);
            db.Entry(target).Property(nameof(VeiculoMidia.NomeArquivo)).CurrentValue = item.NomeArquivoDestino;
            db.Entry(target).Property(nameof(VeiculoMidia.Ordem)).CurrentValue = item.Ordem;
        }

        if (item.Capa)
        {
            foreach (var media in activeImages)
            {
                db.Entry(media).Property(nameof(VeiculoMidia.Capa)).CurrentValue = false;
            }

            db.Entry(target).Property(nameof(VeiculoMidia.Capa)).CurrentValue = true;
        }

        target.UpdateStorage(uploaded.Stored.Key, uploaded.Stored.Container, uploaded.ContentType, uploaded.SizeBytes);
        item.AttachMedia(target.Id == 0 ? null : target.Id);
        item.UpdateDestination(uploaded.Stored.Container, uploaded.Stored.Url, uploaded.ContentType, uploaded.SizeBytes);
        item.MarkSucceeded();
        AddLog(item, "Banco", "Sucesso", "Registro atualizado apos confirmacao do upload.");
        await db.SaveChangesAsync(ct);

        if (item.VeiculoMidiaId is null && target.Id > 0)
        {
            item.AttachMedia(target.Id);
            await db.SaveChangesAsync(ct);
        }

        await transaction.CommitAsync(ct);

        if (job.Sobrescrever
            && !string.IsNullOrWhiteSpace(oldBlobName)
            && !string.Equals(oldBlobName.Trim().Replace('\\', '/'), uploaded.Stored.Key, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await storage.DeleteAsync(oldBlobName, ct);
                AddLog(item, "Limpeza", "Sucesso", $"Blob anterior removido: {oldBlobName}.");
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Nao foi possivel remover blob antigo {BlobName}.", oldBlobName);
                AddLog(item, "Limpeza", "Aviso", $"Upload concluido, mas nao foi possivel remover blob anterior: {ex.Message}");
                await db.SaveChangesAsync(ct);
            }
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
        var client = httpClientFactory.CreateClient(LegacyVehicleImageImportService.HttpClientName);

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

    private void AddLog(ImportJobItem item, string stage, string status, string message)
        => db.ImportJobLogs.Add(new ImportJobLog(item.ImportJobId, item.Id, item.VeiculoId, item.Ordem + 1, item.UrlLegada, stage, status, message));

    private static void ValidateMetadata(StoredFile stored, StorageObjectMetadata metadata, long bytesRead, string contentType)
    {
        if (!string.Equals(metadata.Key, stored.Key, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("HEAD retornou chave divergente.");
        }

        if (!string.Equals(metadata.Container, stored.Container, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("HEAD retornou bucket/container divergente.");
        }

        if (metadata.SizeBytes.HasValue && metadata.SizeBytes.Value != bytesRead)
        {
            throw new InvalidOperationException($"Tamanho divergente no storage. Local={bytesRead}; Remoto={metadata.SizeBytes.Value}.");
        }

        if (!string.IsNullOrWhiteSpace(metadata.ContentType)
            && !metadata.ContentType.StartsWith(contentType, StringComparison.OrdinalIgnoreCase)
            && !contentType.StartsWith(metadata.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Content-Type divergente no storage. Local={contentType}; Remoto={metadata.ContentType}.");
        }
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private sealed record UploadedImage(StoredFile Stored, string ContentType, long SizeBytes);

    private sealed class CountingReadStream(Stream inner) : Stream
    {
        public long BytesRead { get; private set; }
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
