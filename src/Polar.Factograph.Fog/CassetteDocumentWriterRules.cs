using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentWriterRules
{
    public static void RequireWritable(CassetteDefinition cassette)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        if (!cassette.Enabled || !cassette.AllowWrite)
        {
            throw new InvalidOperationException($"Cassette is not writable: {cassette.Id}");
        }
    }

    public static string RequireOriginal(
        CassetteDefinition cassette,
        CassetteDocumentLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        if (!string.Equals(cassette.Id, location.CassetteId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Document location belongs to another cassette.",
                nameof(location));
        }

        return location.OriginalPath
            ?? throw new KeyNotFoundException(
                $"Document original was not found: {location.DocumentUri}");
    }
}
