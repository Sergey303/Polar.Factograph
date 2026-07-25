namespace Polar.Factograph.Application;

internal static class OntologyLocalization
{
    public static string? Select(
        IReadOnlyList<OntologyLocalizedText> values,
        string preferredLanguage)
    {
        if (values.Count == 0)
        {
            return null;
        }

        return values.FirstOrDefault(value =>
                   string.Equals(value.Language, preferredLanguage, StringComparison.OrdinalIgnoreCase))?.Value
               ?? values.FirstOrDefault(value => value.Language is null)?.Value
               ?? values.FirstOrDefault(value =>
                   string.Equals(value.Language, "ru", StringComparison.OrdinalIgnoreCase))?.Value
               ?? values.FirstOrDefault(value =>
                   string.Equals(value.Language, "en", StringComparison.OrdinalIgnoreCase))?.Value
               ?? values[0].Value;
    }
}
