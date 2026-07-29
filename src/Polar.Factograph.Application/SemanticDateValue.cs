using System.Globalization;
using System.Text.RegularExpressions;

namespace Polar.Factograph.Application;

internal sealed record SemanticDateValue(string Display, string SortKey);

internal static partial class SemanticDateParser
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly IReadOnlyDictionary<string, int> RussianMonths =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["янв"] = 1,
            ["фев"] = 2,
            ["мар"] = 3,
            ["апр"] = 4,
            ["май"] = 5,
            ["мая"] = 5,
            ["июн"] = 6,
            ["июл"] = 7,
            ["авг"] = 8,
            ["сен"] = 9,
            ["сент"] = 9,
            ["окт"] = 10,
            ["ноя"] = 11,
            ["дек"] = 12
        };

    public static SemanticDateValue? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string display = value.Trim();
        string compact = Regex.Replace(display.ToLowerInvariant(), @"\s+", string.Empty);

        Match numeric = NumericDate().Match(compact);
        if (numeric.Success &&
            int.TryParse(numeric.Groups["year"].Value, out int year))
        {
            int month = ParsePart(numeric.Groups["month"].Value, 1);
            int day = ParsePart(numeric.Groups["day"].Value, 1);
            if (ValidDate(year, month, day))
            {
                return new SemanticDateValue(display, SortKey(year, month, day));
            }
        }

        Match named = NamedMonthDate().Match(compact);
        if (named.Success &&
            int.TryParse(named.Groups["year"].Value, out year) &&
            RussianMonths.TryGetValue(named.Groups["month"].Value, out int namedMonth))
        {
            int day = ParsePart(named.Groups["day"].Value, 1);
            if (ValidDate(year, namedMonth, day))
            {
                return new SemanticDateValue(display, SortKey(year, namedMonth, day));
            }
        }

        if (DateTimeOffset.TryParse(
                display,
                RussianCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTimeOffset parsed) ||
            DateTimeOffset.TryParse(
                display,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out parsed))
        {
            return new SemanticDateValue(
                display,
                SortKey(parsed.Year, parsed.Month, parsed.Day));
        }

        Match leadingYear = LeadingYear().Match(compact);
        return leadingYear.Success && int.TryParse(leadingYear.Groups["year"].Value, out year)
            ? new SemanticDateValue(display, SortKey(year, 1, 1))
            : null;
    }

    private static int ParsePart(string value, int fallback) =>
        int.TryParse(value, out int parsed) ? parsed : fallback;

    private static bool ValidDate(int year, int month, int day) =>
        year is >= 1 and <= 9999 &&
        month is >= 1 and <= 12 &&
        day >= 1 &&
        day <= DateTime.DaysInMonth(year, month);

    private static string SortKey(int year, int month, int day) =>
        FormattableString.Invariant($"{year:D4}-{month:D2}-{day:D2}");

    [GeneratedRegex(@"^(?<year>\d{4})(?:[-./]?(?<month>\d{1,2})(?:[-./]?(?<day>\d{1,2}))?)?$")]
    private static partial Regex NumericDate();

    [GeneratedRegex(@"^(?<year>\d{4})(?<month>[а-яё]{3,4})(?<day>\d{1,2})?$")]
    private static partial Regex NamedMonthDate();

    [GeneratedRegex(@"^(?<year>\d{4})")]
    private static partial Regex LeadingYear();
}
