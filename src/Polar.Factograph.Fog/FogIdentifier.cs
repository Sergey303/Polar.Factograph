namespace Polar.Factograph.Fog;

public static class FogIdentifier
{
    internal static string Require(
        string? value,
        string fogPath,
        string recordDescription)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Fog {recordDescription} record has no identifier: {fogPath}");
        }

        return Clean(value);
    }

    public static string Clean(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace("|", string.Empty, StringComparison.Ordinal);
    }
}
