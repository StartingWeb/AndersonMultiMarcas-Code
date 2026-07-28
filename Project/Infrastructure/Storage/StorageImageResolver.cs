using Core.Storage;
using Microsoft.Extensions.Options;
using Project.Shared;

namespace Project.Infrastructure.Storage;

public sealed class StorageImageResolver(
    LocalWebRootStorageService local,
    R2StorageService r2,
    IOptions<StorageOptions> options) : IStorageImageResolver
{
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

        if (ShouldReadR2First)
        {
            return r2.GetPublicUrl(key);
        }

        if (await local.ExistsAsync(key, ct))
        {
            return local.GetPublicUrl(key);
        }

        return IsAbsoluteHttpUrl(normalized) ? normalized : null;
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
