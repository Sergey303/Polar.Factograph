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

        DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out DateTime parsed) ||
            DateTime.TryParse(value, RussianCulture, styles, out parsed) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"Fog mT value cannot be parsed in '{fogPath}': {value}");
    }
}
