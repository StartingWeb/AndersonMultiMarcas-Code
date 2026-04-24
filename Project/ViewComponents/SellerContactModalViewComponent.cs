using Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Project.ViewComponents;

public sealed class SellerContactModalViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public SellerContactModalViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var sellers = await _context.Vendedores
            .AsNoTracking()
            .Where(vendedor => vendedor.Ativo)
            .OrderBy(vendedor => vendedor.Nome)
            .Select(vendedor => new SellerContactItem
            {
                Nome = string.IsNullOrWhiteSpace(vendedor.Nome) ? "Vendedor" : vendedor.Nome!,
                Telefone = NormalizePhone(vendedor.Telefone),
                Whatsapp = NormalizePhone(vendedor.Whatsapp),
                FotoUrl = NormalizePhoto(vendedor.FotoUrl)
            })
            .ToListAsync();

        return View(new SellerContactModalViewModel
        {
            Sellers = sellers
        });
    }

    private static string NormalizePhoto(string? fotoUrl)
    {
        if (string.IsNullOrWhiteSpace(fotoUrl))
        {
            return "/img/logo.png";
        }

        if (Uri.TryCreate(fotoUrl, UriKind.Absolute, out _))
        {
            return OptimizeExternalImageUrl(fotoUrl, 96, 96);
        }

        var normalized = fotoUrl.StartsWith('/')
            ? fotoUrl
            : $"/{fotoUrl.TrimStart('/')}";

        return normalized;
    }

    private static string OptimizeExternalImageUrl(string imageUrl, int width, int height)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
        {
            return imageUrl;
        }

        var host = absoluteUri.Host.ToLowerInvariant();

        if (host.Contains("res.cloudinary.com", StringComparison.Ordinal))
        {
            const string marker = "/upload/";
            var markerIndex = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                var prefix = imageUrl[..(markerIndex + marker.Length)];
                var suffix = imageUrl[(markerIndex + marker.Length)..];
                return $"{prefix}c_fill,f_auto,q_auto,w_{width},h_{height}/{suffix}";
            }
        }

        if (host.Contains("imgix.net", StringComparison.Ordinal))
        {
            return AppendQueryString(imageUrl, $"auto=format,compress&fit=crop&w={width}&h={height}&q=70");
        }

        if (host.Contains("imagekit.io", StringComparison.Ordinal))
        {
            return AppendQueryString(imageUrl, $"tr=w-{width},h-{height},c-at_max,f-auto,q-70");
        }

        return imageUrl;
    }

    private static string AppendQueryString(string url, string query)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}{query}";
    }

    private static string? NormalizePhone(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
        {
            return null;
        }

        return digits.StartsWith("55", StringComparison.Ordinal) ? digits : $"55{digits}";
    }

    public sealed class SellerContactModalViewModel
    {
        public IReadOnlyList<SellerContactItem> Sellers { get; init; } = [];
    }

    public sealed class SellerContactItem
    {
        public string Nome { get; init; } = "Vendedor";
        public string? Telefone { get; init; }
        public string? Whatsapp { get; init; }
        public string FotoUrl { get; init; } = "/img/logo.png";
    }
}
