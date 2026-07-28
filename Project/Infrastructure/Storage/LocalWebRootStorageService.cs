using Core.Storage;

namespace Project.Infrastructure.Storage;

public sealed class LocalWebRootStorageService(IWebHostEnvironment environment) : IStorageService
{
    public async Task<StoredFile> SaveAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var normalizedKey = StoragePath.NormalizeKey(key);
        var fullPath = ResolveFullPath(normalizedKey);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using (var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true))
        {
            await content.CopyToAsync(output, ct);
        }

        var info = new FileInfo(fullPath);
        return new StoredFile(
            normalizedKey,
            GetPublicUrl(normalizedKey),
            StoragePath.GetFileName(normalizedKey),
            StoragePath.GetContainer(normalizedKey),
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            info.Length);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct)
    {
        _ = ct;
        return Task.FromResult(File.Exists(ResolveFullPath(StoragePath.NormalizeKey(key))));
    }

    public Task<StorageObjectMetadata?> GetMetadataAsync(string key, CancellationToken ct)
    {
        _ = ct;
        var normalizedKey = StoragePath.NormalizeKey(key);
        var fullPath = ResolveFullPath(normalizedKey);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<StorageObjectMetadata?>(null);
        }

        var info = new FileInfo(fullPath);
        var metadata = new StorageObjectMetadata(
            normalizedKey,
            StoragePath.GetContainer(normalizedKey),
            null,
            info.Length,
            info.LastWriteTimeUtc,
            null);

        return Task.FromResult<StorageObjectMetadata?>(metadata);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct)
    {
        _ = ct;
        var fullPath = ResolveFullPath(StoragePath.NormalizeKey(key));
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 128 * 1024, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        _ = ct;
        var fullPath = ResolveFullPath(StoragePath.NormalizeKey(key));
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public string GetPublicUrl(string key)
        => StoragePath.ToPublicPath(key);

    public string ResolveFullPath(string key)
    {
        var root = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
        var relative = StoragePath.NormalizeKey(key).Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(root, relative));
        var rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de storage local fora de wwwroot.");
        }

        return fullPath;
    }
}
