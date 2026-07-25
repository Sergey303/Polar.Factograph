namespace Polar.Factograph.Application;

internal static class ProjectConfigurationValidation
{
    public static void RequireText(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(message);
        }
    }

    public static void RejectDuplicates(
        IReadOnlyList<string> values,
        string location)
    {
        string? duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate value '{duplicate}' in {location}.");
        }
    }
}
