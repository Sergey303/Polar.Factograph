using System.Globalization;

namespace Polar.Factograph.Fog;

internal static class LegacyFogTime
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static DateTime Parse(string? value, string fogPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.MinValue;
        }

        DateTimeStyles offsetStyles =
            DateTimeStyles.AllowWhiteSpaces |
            DateTimeStyles.AssumeUniversal |
            DateTimeStyles.AdjustToUniversal;
        if (DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                offsetStyles,
                out DateTimeOffset offset) ||
            DateTimeOffset.TryParse(value, RussianCulture, offsetStyles, out offset) ||
            DateTimeOffset.TryParse(
                value,
                CultureInfo.CurrentCulture,
                offsetStyles,
                out offset))
        {
            return offset.UtcDateTime;
        }

        throw new InvalidDataException($"Fog mT value cannot be parsed in '{fogPath}': {value}");
    }
}
