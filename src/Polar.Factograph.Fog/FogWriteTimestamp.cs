namespace Polar.Factograph.Fog;

internal static class FogWriteTimestamp
{
    public static DateTime Resolve(
        DateTime requestedUtc,
        DateTime? latestExisting)
    {
        DateTime requested = AsUtc(requestedUtc);
        if (latestExisting is null)
        {
            return requested;
        }

        DateTime latest = AsUtc(latestExisting.Value);
        return latest < requested
            ? requested
            : latest.AddSeconds(1);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
