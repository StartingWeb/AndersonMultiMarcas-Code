using Core.Storage;

namespace Project.Infrastructure.Storage;

public interface IStorageImageResolver
{
    Task<IReadOnlyList<string>> ResolveVehicleGalleryAsync(
        IEnumerable<StorageImageReference> references,
        bool includeDefault,
        CancellationToken ct);

    Task<string> SelectVehicleCoverAsync(IEnumerable<StorageImageReference> references, CancellationToken ct);

    Task<string?> ResolveSellerPhotoAsync(string? source, CancellationToken ct);
}
