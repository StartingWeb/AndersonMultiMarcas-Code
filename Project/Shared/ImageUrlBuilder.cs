using Microsoft.AspNetCore.WebUtilities;

namespace Project.Shared;

public static class ImageUrlBuilder
{
    public static string Build(string? source, int width = 0, int quality = 68)
    {
        if (!CanOptimize(source))
        {
            return source ?? string.Empty;
        }

        var query = new Dictionary<string, string?>
        {
            ["src"] = source,
            ["q"] = Math.Clamp(quality, 35, 90).ToString()
        };

        if (width > 0)
        {
            query["w"] = width.ToString();
        }

        return QueryHelpers.AddQueryString("/media/img", query);
    }

    public static string BuildSrcSet(string? source, params int[] widths)
    {
        if (!CanOptimize(source) || widths.Length == 0)
        {
            return string.Empty;
        }

        return string.Join(", ", widths
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .Select(x => $"{Build(source, x)} {x}w"));
    }

    private static bool CanOptimize(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || !source.StartsWith('/'))
        {
            return false;
        }

        var lower = source.Split('?', '#')[0].ToLowerInvariant();
        return lower.EndsWith(".jpg")
            || lower.EndsWith(".jpeg")
            || lower.EndsWith(".png")
            || lower.EndsWith(".webp");
    }
}
