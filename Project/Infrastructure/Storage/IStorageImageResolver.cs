using Core.Storage;

namespace Project.Infrastructure.Storage;

public interface IStorageImageResolver
{
    IReadOnlyList<string> ResolveVehicleGallery(
        IEnumerable<StorageImageReference> references,
        bool includeDefault);

    string SelectVehicleCover(IEnumerable<StorageImageReference> references);

    string? ResolveSellerPhoto(string? source);

    Task<IReadOnlyList<string>> ResolveVehicleGalleryAsync(
        IEnumerable<StorageImageReference> references,
        bool includeDefault,
        CancellationToken ct);

    Task<string> SelectVehicleCoverAsync(IEnumerable<StorageImageReference> references, CancellationToken ct);

    Task<string?> ResolveSellerPhotoAsync(string? source, CancellationToken ct);
}
