using System.Globalization;

namespace Fauzora.Extensions;

public static class CurrencyExtensions
{
    private static readonly CultureInfo Indonesia = new("id-ID");

    public static string ToRupiah(this decimal value) => "Rp " + value.ToString("N0", Indonesia);
}
