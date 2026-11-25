namespace Fauzora.Extensions;

public static class RatingExtensions
{
    public static string ToRatingNumber(this decimal rating) =>
        Math.Clamp(rating, 0, 5).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
}