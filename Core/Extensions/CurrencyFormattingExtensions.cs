using System.Globalization;

namespace Core.Extensions;

public static class CurrencyFormattingExtensions
{
    private static readonly CultureInfo PtBrCulture = new("pt-BR");

    public static string ToBrazilianCurrency(this decimal value)
    {
        return value.ToString("C2", PtBrCulture);
    }

    public static string ToBrazilianCurrency(this decimal? value, string fallback = "")
    {
        return value.HasValue ? value.Value.ToBrazilianCurrency() : fallback;
    }
}
