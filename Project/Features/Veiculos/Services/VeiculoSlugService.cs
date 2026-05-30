using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Project.Features.Veiculos.Services;

public sealed class VeiculoSlugService : IVeiculoSlugService
{
    public string CriarSlug(string titulo, string modelo, string? versao, int id)
    {
        var baseTexto = $"{titulo} {modelo} {versao}".Trim();
        var slug = Slugify(baseTexto);
        return $"{slug}-{id}";
    }

    public int? ObterIdPorSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var partes = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0) return null;
        return int.TryParse(partes[^1], out var id) ? id : null;
    }

    private static string Slugify(string texto)
    {
        var normalized = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, "[^a-z0-9]+", "-");
        cleaned = cleaned.Trim('-');
        return string.IsNullOrWhiteSpace(cleaned) ? "veiculo" : cleaned;
    }
}
