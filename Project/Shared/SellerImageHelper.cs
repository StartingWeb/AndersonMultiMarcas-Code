namespace Project.Shared;

public static class SellerImageHelper
{
    public static string? Normalize(string? source)
    {
        var normalized = NormalizePath(source);
        if (IsSellerImage(normalized))
        {
            return normalized;
        }

        return null;
    }

    public static bool IsSellerImage(string? source)
    {
        var normalized = NormalizePath(source);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.StartsWith("/uploads/veiculos/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("carroDefault", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return normalized.StartsWith("/uploads/vendedores/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("/img/Vendedores", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var path = source.Trim().Replace('\\', '/');

        if (path.StartsWith("~/", StringComparison.Ordinal))
        {
            path = path[1..];
        }

        if (path.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["wwwroot".Length..];
        }

        if (path.StartsWith("uploads/vendedores/", StringComparison.OrdinalIgnoreCase))
        {
            path = "/" + path;
        }

        return path;
    }
}
