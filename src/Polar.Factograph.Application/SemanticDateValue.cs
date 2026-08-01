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

        string source = value.Trim();
        string compact = Whitespace().Replace(source.ToLowerInvariant(), string.Empty);

        Match numeric = NumericDate().Match(compact);
        if (numeric.Success &&
            int.TryParse(numeric.Groups["year"].Value, out int year))
        {
            bool hasMonth = numeric.Groups["month"].Success;
            bool hasDay = numeric.Groups["day"].Success;
            int month = ParsePart(numeric.Groups["month"].Value, 1);
            int day = ParsePart(numeric.Groups["day"].Value, 1);
            if (ValidDate(year, month, day))
            {
                return new SemanticDateValue(
                    Format(year, hasMonth ? month : null, hasDay ? day : null),
                    SortKey(year, month, day));
            }
        }

        Match named = NamedMonthDate().Match(compact);
        if (named.Success &&
            int.TryParse(named.Groups["year"].Value, out year) &&
            RussianMonths.TryGetValue(named.Groups["month"].Value, out int namedMonth))
        {
            bool hasDay = named.Groups["day"].Success;
            int day = ParsePart(named.Groups["day"].Value, 1);
            if (ValidDate(year, namedMonth, day))
            {
                return new SemanticDateValue(
                    Format(year, namedMonth, hasDay ? day : null),
                    SortKey(year, namedMonth, day));
            }
        }

        if (DateTimeOffset.TryParse(
                source,
                RussianCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out DateTimeOffset parsed) ||
            DateTimeOffset.TryParse(
                source,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out parsed))
        {
            return new SemanticDateValue(
                Format(parsed.Year, parsed.Month, parsed.Day),
                SortKey(parsed.Year, parsed.Month, parsed.Day));
        }

        Match leadingYear = LeadingYear().Match(compact);
        return leadingYear.Success && int.TryParse(leadingYear.Groups["year"].Value, out year)
            ? new SemanticDateValue(Format(year, null, null), SortKey(year, 1, 1))
            : null;
    }

    private static string Format(int year, int? month, int? day)
    {
        if (month is null)
        {
            return $"{year.ToString(CultureInfo.InvariantCulture)} г.";
        }

        if (day is null)
        {
            string monthName = RussianCulture.DateTimeFormat.MonthNames[month.Value - 1];
            string titledMonth = RussianCulture.TextInfo.ToTitleCase(monthName);
            return $"{titledMonth} {year.ToString(CultureInfo.InvariantCulture)} г.";
        }

        string genitiveMonth = RussianCulture.DateTimeFormat.MonthGenitiveNames[month.Value - 1];
        return $"{day.Value.ToString(CultureInfo.InvariantCulture)} {genitiveMonth} {year.ToString(CultureInfo.InvariantCulture)} г.";
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

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"^(?<year>\d{4})(?:[-./]?(?<month>\d{1,2})(?:[-./]?(?<day>\d{1,2}))?)?$")]
    private static partial Regex NumericDate();

    [GeneratedRegex(@"^(?<year>\d{4})(?<month>[а-яё]{3,4})(?<day>\d{1,2})?$")]
    private static partial Regex NamedMonthDate();

    [GeneratedRegex(@"^(?<year>\d{4})")]
    private static partial Regex LeadingYear();
}
