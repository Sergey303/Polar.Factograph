using System.Security.Cryptography;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

internal static class PreviewOriginalVersion
{
    public static async Task<bool> MatchesAsync(
        string path,
        CassettePreviewRequest request,
        CancellationToken cancellationToken)
    {
        FileInfo file = new(path);
        if (!file.Exists || file.Length != request.OriginalLength)
        {
            return false;
        }

        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81_920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        string value = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(value, request.OriginalSha256, StringComparison.OrdinalIgnoreCase);
    }
}
