using Core.Storage;
using Microsoft.Extensions.Options;
using Project.Shared;

namespace Project.Infrastructure.Storage;

public sealed class StorageImageResolver(IOptions<StorageOptions> options) : IStorageImageResolver
{
    public IReadOnlyList<string> ResolveVehicleGallery(
        IEnumerable<StorageImageReference> references,
        bool includeDefault)
    {
        var images = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in references)
        {
            var resolved = ResolveVehicleImage(reference);
            if (string.IsNullOrWhiteSpace(resolved)
                || string.Equals(resolved, VehicleImageHelper.DefaultVehicleImage, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (seen.Add(GetDistinctKey(resolved)))
            {
                images.Add(resolved);
            }
        }

        if (images.Count == 0 && includeDefault)
        {
            images.Add(VehicleImageHelper.DefaultVehicleImage);
        }

        return images;
    }

    public string SelectVehicleCover(IEnumerable<StorageImageReference> references)
        => ResolveVehicleGallery(references, includeDefault: true).First();

    public string? ResolveSellerPhoto(string? source)
    {
        var normalized = SellerImageHelper.Normalize(source);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return ResolveSellerImage(new StorageImageReference(normalized));
    }

    public Task<IReadOnlyList<string>> ResolveVehicleGalleryAsync(
        IEnumerable<StorageImageReference> references,
        bool includeDefault,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ResolveVehicleGallery(references, includeDefault));
    }

    public Task<string> SelectVehicleCoverAsync(IEnumerable<StorageImageReference> references, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(SelectVehicleCover(references));
    }

    public Task<string?> ResolveSellerPhotoAsync(string? source, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ResolveSellerPhoto(source));
    }

    private string? ResolveVehicleImage(StorageImageReference reference)
    {
        if (TryGetExplicitStorageKey(reference, out var key) && StoragePath.IsVehicleKey(key))
        {
            return BuildPublicUrl(key);
        }

        if (!VehicleImageHelper.TryNormalize(reference.Url, out var normalized))
        {
            return null;
        }

        return normalized;
    }

    private string? ResolveSellerImage(StorageImageReference reference)
    {
        if (TryGetExplicitStorageKey(reference, out var key) && StoragePath.IsSellerKey(key))
        {
            return BuildPublicUrl(key);
        }

        var normalized = SellerImageHelper.Normalize(reference.Url);
        if (string.IsNullOrWhiteSpace(normalized) || IsAbsoluteHttpUrl(normalized))
        {
            return normalized;
        }

        return StoragePath.TryGetKeyFromSource(normalized, [], out key) && StoragePath.IsSellerKey(key)
            ? BuildPublicUrl(key)
            : normalized;
    }

    private string BuildPublicUrl(string key)
    {
        var normalizedKey = StoragePath.NormalizeKey(key);
        var publicBaseUrl = options.Value.R2.PublicBaseUrl;
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            publicBaseUrl = options.Value.PublicBaseUrl;
        }

        return string.IsNullOrWhiteSpace(publicBaseUrl)
            ? StoragePath.ToPublicPath(normalizedKey)
            : $"{publicBaseUrl.TrimEnd('/')}/{normalizedKey}";
    }

    private static bool TryGetExplicitStorageKey(StorageImageReference reference, out string key)
    {
        key = string.Empty;

        if (!string.IsNullOrWhiteSpace(reference.BlobName))
        {
            var blobName = reference.BlobName.Trim();
            if (blobName.Contains('/') || blobName.Contains('\\'))
            {
                return TryNormalizeKey(blobName, out key);
            }

            if (!string.IsNullOrWhiteSpace(reference.Container))
            {
                return TryNormalizeKey(StoragePath.Combine(reference.Container, blobName), out key);
            }
        }

        if (!string.IsNullOrWhiteSpace(reference.NomeArquivo)
            && !string.IsNullOrWhiteSpace(reference.Container))
        {
            return TryNormalizeKey(StoragePath.Combine(reference.Container, reference.NomeArquivo), out key);
        }

        return false;
    }

    private static bool TryNormalizeKey(string value, out string key)
    {
        key = string.Empty;
        try
        {
            key = StoragePath.NormalizeKey(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static string GetDistinctKey(string source)
    {
        if (StoragePath.TryGetKeyFromSource(source, [], out var key))
        {
            return key;
        }

        return source.Split('?', '#')[0].TrimEnd('/').ToUpperInvariant();
    }

    private static bool IsAbsoluteHttpUrl(string? source)
        => !string.IsNullOrWhiteSpace(source)
            && (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
}
