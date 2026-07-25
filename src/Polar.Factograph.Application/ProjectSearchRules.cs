using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal static class ProjectSearchRules
{
    private const string SystemCassetteId = "$system";

    public static void Validate(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit,
        string preferredLanguage)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                "Search limit must be between 1 and 500.");
        }
    }

    public static HashSet<string> EffectiveCassetteIds(
        IReadOnlySet<string> allowedCassetteIds) =>
        new(allowedCassetteIds, StringComparer.Ordinal)
        {
            SystemCassetteId
        };

    public static int NameScore(string value, string searchKey)
    {
        string normalized = LegacySearchIndexProjector.NormalizeNameQuery(value);
        if (string.Equals(normalized, searchKey, StringComparison.Ordinal)) return 3;
        return normalized.StartsWith(searchKey, StringComparison.Ordinal) ? 2 : 1;
    }

    public static string SelectDisplayName(
        IReadOnlyList<NameSearchHit> names,
        string preferredLanguage,
        string fallbackResourceId) => names
        .OrderBy(hit => string.Equals(
            hit.Predicate,
            LegacySearchIndexProjector.NamePredicate,
            StringComparison.Ordinal) ? 0 : 1)
        .ThenBy(hit => LanguagePriority(hit.Language, preferredLanguage))
        .ThenBy(hit => hit.Value, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault()
        ?.Value ?? fallbackResourceId;

    public static ProjectSearchEvidence ToEvidence(NameSearchHit hit) => new(
        hit.Predicate,
        hit.Value,
        hit.Language);

    public static ProjectSearchEvidence ToEvidence(WordSearchHit hit) => new(
        hit.Predicate,
        hit.Value,
        hit.Language);

    private static int LanguagePriority(string? language, string preferredLanguage)
    {
        if (string.Equals(language, preferredLanguage, StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.IsNullOrEmpty(language)) return 1;
        if (string.Equals(language, "ru", StringComparison.OrdinalIgnoreCase)) return 2;
        if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)) return 3;
        return 4;
    }
}
