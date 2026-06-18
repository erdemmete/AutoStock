using System.Globalization;

namespace AutoStock.WEB.Helpers;

public static class SenteMoney
{
    private static readonly CultureInfo TrCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Format(decimal value)
    {
        var format = decimal.Truncate(value) == value ? "N0" : "N2";

        return value.ToString(format, TrCulture) + " ₺";
    }

    public static string Format(decimal? value, string fallback = "-")
    {
        return value.HasValue ? Format(value.Value) : fallback;
    }
}
