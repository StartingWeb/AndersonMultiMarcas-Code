using Core.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Project.Shared;

namespace Project.Infrastructure.Storage;

public sealed class StorageImageResolver(
    LocalWebRootStorageService local,
    R2StorageService r2,
    IOptions<StorageOptions> options,
    IMemoryCache cache,
    ILogger<StorageImageResolver> logger) : IStorageImageResolver
{
    private static readonly TimeSpan PositiveRemoteCacheDuration = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan NegativeRemoteCacheDuration = TimeSpan.FromMinutes(3);

    public async Task<IReadOnlyList<string>> ResolveVehicleGalleryAsync(
        IEnumerable<StorageImageReference> references,
        bool includeDefault,
        CancellationToken ct)
    {
        var images = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var reference in references)
        {
            var resolved = await ResolveImageAsync(reference, ImageKind.Vehicle, ct);
            if (string.IsNullOrWhiteSpace(resolved)
                || string.Equals(resolved, VehicleImageHelper.DefaultVehicleImage, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var key = GetDistinctKey(resolved);
            if (seen.Add(key))
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

    public async Task<string> SelectVehicleCoverAsync(IEnumerable<StorageImageReference> references, CancellationToken ct)
        => (await ResolveVehicleGalleryAsync(references, includeDefault: true, ct)).First();

    public async Task<string?> ResolveSellerPhotoAsync(string? source, CancellationToken ct)
    {
        var normalized = SellerImageHelper.Normalize(source);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await ResolveImageAsync(new StorageImageReference(normalized), ImageKind.Seller, ct);
    }

    private async Task<string?> ResolveImageAsync(StorageImageReference reference, ImageKind kind, CancellationToken ct)
    {
        var normalized = kind == ImageKind.Vehicle
            ? (VehicleImageHelper.TryNormalize(reference.Url, out var vehicleUrl) ? vehicleUrl : null)
            : SellerImageHelper.Normalize(reference.Url);

        var lookupReference = reference with { Url = normalized ?? reference.Url };
        if (!StoragePath.TryGetKey(lookupReference, PublicBaseUrls(), out var key))
        {
            return IsAbsoluteHttpUrl(normalized) ? normalized : null;
        }

        if (kind == ImageKind.Vehicle && !StoragePath.IsVehicleKey(key))
        {
            return null;
        }

        if (kind == ImageKind.Seller && !StoragePath.IsSellerKey(key))
        {
            return IsAbsoluteHttpUrl(normalized) ? normalized : null;
        }

        if (await RemoteExistsAsync(key, ct))
        {
            return r2.GetPublicUrl(key);
        }

        if (await local.ExistsAsync(key, ct))
        {
            return local.GetPublicUrl(key);
        }

        return IsAbsoluteHttpUrl(normalized) ? normalized : null;
    }

    private async Task<bool> RemoteExistsAsync(string key, CancellationToken ct)
    {
        if (!ShouldReadR2First)
        {
            return false;
        }

        var normalizedKey = StoragePath.NormalizeKey(key);
        var cacheKey = $"storage:r2:exists:{normalizedKey}";
        try
        {
            return await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                var exists = await r2.ExistsAsync(normalizedKey, ct);
                entry.AbsoluteExpirationRelativeToNow = exists
                    ? PositiveRemoteCacheDuration
                    : NegativeRemoteCacheDuration;
                return exists;
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Nao foi possivel validar {StorageKey} no R2. Usando fallback local.", normalizedKey);
            return false;
        }
    }

    private IEnumerable<string?> PublicBaseUrls()
    {
        yield return options.Value.PublicBaseUrl;
        yield return options.Value.R2.PublicBaseUrl;
        yield return options.Value.R2.ServiceUrl;
    }

    private bool ShouldReadR2First => r2.IsConfigured && (options.Value.DualReadEnabled || options.Value.UseR2ForWrites);

    private static bool IsAbsoluteHttpUrl(string? source)
        => !string.IsNullOrWhiteSpace(source)
            && (source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

    private static string GetDistinctKey(string source)
    {
        if (StoragePath.TryGetKeyFromSource(source, [], out var key))
        {
            return key;
        }

        return source.Split('?', '#')[0].TrimEnd('/').ToUpperInvariant();
    }

    private enum ImageKind
    {
        Vehicle,
        Seller
    }
}
