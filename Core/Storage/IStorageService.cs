namespace Core.Storage;

public interface IStorageService
{
    Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<bool> ExistsAsync(string key, CancellationToken ct);
    Task<StorageObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string key, CancellationToken ct);
    Task DeleteAsync(string key, CancellationToken ct);
    string GetPublicUrl(string key);
}

public sealed record StoredFile(
    string Key,
    string Url,
    string FileName,
    string Container,
    string ContentType,
    long SizeBytes);

public sealed record StorageObjectMetadata(
    string Key,
    string Container,
    string? ContentType,
    long? SizeBytes,
    DateTimeOffset? LastModified,
    string? ETag);
