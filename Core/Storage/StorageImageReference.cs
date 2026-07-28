namespace Core.Storage;

public sealed record StorageImageReference(
    string? Url,
    string? BlobName = null,
    string? Container = null,
    string? NomeArquivo = null,
    string? ContentType = null,
    long? SizeBytes = null);
