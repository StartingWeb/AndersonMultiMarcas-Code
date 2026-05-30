using Microsoft.AspNetCore.WebUtilities;

namespace Project.Shared;

public static class VehicleImageHelper
{
    public const string DefaultVehicleImage = "/img/carroDefault.png";

    public static string Normalize(string? source)
        => TryNormalize(source, out var normalized) ? normalized : DefaultVehicleImage;

    public static bool TryNormalize(string? source, out string normalized)
    {
        normalized = NormalizePath(source);
        return IsVehicleImage(normalized);
    }

    public static IReadOnlyList<string> NormalizeGallery(IEnumerable<string?> sources, bool includeDefault = true, string? webRootPath = null)
    {
        var images = sources
            .SelectMany(source => ResolveImageCandidates(source, webRootPath))
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Cast<string>()
            .Where(source => ExistsOrIsRemote(source, webRootPath))
            .Where(source => !string.Equals(source, DefaultVehicleImage, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(GetImageKey)
            .ToList();

        if (images.Count == 0 && includeDefault)
        {
            images.Add(DefaultVehicleImage);
        }

        return images;
    }

    public static string SelectCover(IEnumerable<string?> sources, string? webRootPath = null)
        => NormalizeGallery(sources, webRootPath: webRootPath).First();

    public static bool IsVehicleImage(string? source)
    {
        var normalized = NormalizePath(source);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var lower = normalized.ToLowerInvariant();

        if (lower.Contains("/uploads/vendedores/") || lower.Contains("/img/vendedores"))
        {
            return false;
        }

        if (lower.StartsWith("http://") || lower.StartsWith("https://"))
        {
            return lower.Contains("/uploads/veiculos/", StringComparison.OrdinalIgnoreCase);
        }

        return normalized.StartsWith("/uploads/veiculos/", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals(DefaultVehicleImage, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? source, int depth = 0)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var path = source.Trim().Replace('\\', '/');

        if (depth < 2 && path.StartsWith("/media/img", StringComparison.OrdinalIgnoreCase))
        {
            var queryStart = path.IndexOf('?');
            if (queryStart >= 0)
            {
                var query = QueryHelpers.ParseQuery(path[(queryStart + 1)..]);
                if (query.TryGetValue("src", out var values))
                {
                    return NormalizePath(values.FirstOrDefault(), depth + 1);
                }
            }
        }

        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            path = path[1..];
        }

        if (path.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["wwwroot".Length..];
        }

        var uploadsIndex = path.IndexOf("/uploads/veiculos/", StringComparison.OrdinalIgnoreCase);
        if (uploadsIndex >= 0)
        {
            path = path[uploadsIndex..];
        }

        if (path.StartsWith("uploads/veiculos/", StringComparison.OrdinalIgnoreCase))
        {
            path = "/" + path;
        }

        return path;
    }

    private static IReadOnlyList<string?> ResolveImageCandidates(string? source, string? webRootPath)
    {
        if (!TryNormalize(source, out var normalized))
        {
            return [];
        }

        if (HasImageExtension(normalized))
        {
            return [normalized];
        }

        if (string.IsNullOrWhiteSpace(webRootPath) || !normalized.StartsWith('/'))
        {
            return [];
        }

        var cleanSource = normalized.Split('?', '#')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(webRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, cleanSource));

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(fullPath))
        {
            return [];
        }

        var firstImage = Directory.EnumerateFiles(fullPath)
            .Where(HasImageExtension)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(firstImage))
        {
            return [];
        }

        var relative = Path.GetRelativePath(root, firstImage).Replace(Path.DirectorySeparatorChar, '/');
        return ["/" + relative];
    }

    private static bool HasImageExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetImageKey(string source)
    {
        var normalized = NormalizePath(source);
        var queryStart = normalized.IndexOfAny(['?', '#']);
        if (queryStart >= 0)
        {
            normalized = normalized[..queryStart];
        }

        return normalized.TrimEnd('/').ToUpperInvariant();
    }

    private static bool ExistsOrIsRemote(string source, string? webRootPath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath)
            || source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || string.Equals(source, DefaultVehicleImage, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!source.StartsWith('/'))
        {
            return false;
        }

        var cleanSource = source.Split('?', '#')[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(webRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(root, cleanSource));

        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath);
    }
}
