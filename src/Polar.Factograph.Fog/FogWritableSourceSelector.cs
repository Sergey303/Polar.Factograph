namespace Polar.Factograph.Fog;

public static class FogWritableSourceSelector
{
    public static FogSourceDescriptor Select(
        IEnumerable<FogSourceDescriptor> sources,
        string cassetteId)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);

        return sources
            .Where(source =>
                source.Writable &&
                string.Equals(source.CassetteId, cassetteId, StringComparison.Ordinal))
            .OrderByDescending(source => source.IsCassetteMetadata)
            .ThenBy(source => source.FogPath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Cassette '{cassetteId}' has no writable Fog source.");
    }
}
