using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Polar.Factograph.Fog;

internal static class FogAnonymousResourceIdentifier
{
    private const string Prefix = "urn:polar-factograph:anonymous:";

    public static string Create(
        FogSourceDescriptor source,
        long sourceOrdinal,
        string recordType)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordType);

        string sourceIdentity = GetLogicalSourceIdentity(source.FogPath);
        string value = string.Join(
            "\n",
            source.CassetteId.ToUpperInvariant(),
            source.DatabaseId?.Trim() ?? string.Empty,
            sourceIdentity,
            sourceOrdinal.ToString(CultureInfo.InvariantCulture),
            recordType);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Prefix + Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetLogicalSourceIdentity(string fogPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fogPath);

        string normalized = fogPath.Replace('\\', '/').TrimEnd('/');
        int markerIndex = FindLogicalMarker(normalized, "/originals/");
        if (markerIndex < 0)
        {
            markerIndex = FindLogicalMarker(normalized, "/meta/");
        }

        string logical = markerIndex >= 0
            ? normalized[(markerIndex + 1)..]
            : Path.GetFileName(normalized);
        return logical.ToUpperInvariant();
    }

    private static int FindLogicalMarker(string path, string marker) =>
        path.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
}
