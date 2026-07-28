using Core.Storage;
using Microsoft.Extensions.Options;

namespace Project.Infrastructure.Storage;

public sealed class ApplicationStorageService(
    LocalWebRootStorageService local,
    R2StorageService r2,
    IOptions<StorageOptions> options,
    ILogger<ApplicationStorageService> logger) : IStorageService
{
    public Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
        => UseR2ForWrites
            ? r2.SaveAsync(key, content, contentType, ct)
            : local.SaveAsync(key, content, contentType, ct);

    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
        => await GetMetadataAsync(key, ct) is not null;

    public async Task<StorageObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct)
    {
        if (ShouldReadR2First)
        {
            var remote = await TryGetR2MetadataAsync(key, ct);
            if (remote is not null)
            {
                return remote;
            }
        }

        return await local.GetMetadataAsync(key, ct);
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken ct)
    {
        if (ShouldReadR2First)
        {
            var remote = await TryOpenR2Async(key, ct);
            if (remote is not null)
            {
                return remote;
            }
        }

        return await local.OpenReadAsync(key, ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        var normalizedKey = StoragePath.NormalizeKey(key);

        if (r2.IsConfigured)
        {
            try
            {
                await r2.DeleteAsync(normalizedKey, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Nao foi possivel remover {StorageKey} do R2.", normalizedKey);
            }
        }

        await local.DeleteAsync(normalizedKey, ct);
    }

    public string GetPublicUrl(string key)
        => ShouldReadR2First ? r2.GetPublicUrl(key) : local.GetPublicUrl(key);

    private bool UseR2ForWrites => options.Value.UseR2ForWrites;

    private bool ShouldReadR2First => r2.IsConfigured && (options.Value.DualReadEnabled || UseR2ForWrites);

    private async Task<StorageObjectMetadata?> TryGetR2MetadataAsync(string key, CancellationToken ct)
    {
        try
        {
            return await r2.GetMetadataAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Nao foi possivel consultar metadados de {StorageKey} no R2. Usando fallback local.", key);
            return null;
        }
    }

    private async Task<Stream?> TryOpenR2Async(string key, CancellationToken ct)
    {
        try
        {
            return await r2.OpenReadAsync(key, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Nao foi possivel abrir {StorageKey} no R2. Usando fallback local.", key);
            return null;
        }
    }
}
