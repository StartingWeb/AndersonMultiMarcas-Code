using Core.Storage;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Project.Features.Storage.Legacy;

public interface ILegacyImageStorageVerifier
{
    bool IsConfigured { get; }

    Task<LegacyImageStorageVerification> VerifyAsync(VeiculoMidia media, CancellationToken ct);
}

public sealed record LegacyImageStorageVerification(
    bool Exists,
    string? Key,
    string? PublicUrl,
    string Message);

public sealed class LegacyImageR2StorageVerifier(
    R2StorageService r2,
    IOptions<StorageOptions> storageOptions,
    ILogger<LegacyImageR2StorageVerifier> logger) : ILegacyImageStorageVerifier
{
    public bool IsConfigured => r2.IsConfigured;

    public async Task<LegacyImageStorageVerification> VerifyAsync(VeiculoMidia media, CancellationToken ct)
    {
        if (!r2.IsConfigured)
        {
            return new LegacyImageStorageVerification(false, null, null, "Cloudflare R2 nao esta configurado.");
        }

        if (!StoragePath.TryGetKey(ToStorageReference(media), PublicBaseUrls(), out var key))
        {
            return new LegacyImageStorageVerification(false, null, null, "Chave de storage nao resolvida a partir de BlobName/URL.");
        }

        var publicUrl = r2.GetPublicUrl(key);
        try
        {
            var metadata = await r2.GetMetadataAsync(key, ct);
            return metadata is null
                ? new LegacyImageStorageVerification(false, key, publicUrl, "Objeto ausente no R2.")
                : new LegacyImageStorageVerification(true, key, publicUrl, "Objeto existe no R2.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Erro ao validar existencia do objeto {StorageKey} no R2.", key);
            return new LegacyImageStorageVerification(false, key, publicUrl, $"Erro ao consultar R2: {ex.Message}");
        }
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return storageOptions.Value.PublicBaseUrl;
        yield return storageOptions.Value.R2.PublicBaseUrl;
        yield return storageOptions.Value.R2.ServiceUrl;
    }

    private static StorageImageReference ToStorageReference(VeiculoMidia media)
        => new(
            media.Url,
            media.BlobName,
            media.Container,
            media.NomeArquivo,
            media.ContentType,
            media.TamanhoBytes);
}
