using System.Security.Cryptography;

namespace Polar.Factograph.Fog;

internal sealed record CassetteDocumentCopyResult(long Length, string Sha256);

internal static class CassetteDocumentAtomicFileWriter
{
    private const int BufferSize = 81_920;

    public static async Task<CassetteDocumentCopyResult> WriteAsync(
        Stream source,
        string targetPath,
        long maxBytes,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        string? directory = Path.GetDirectoryName(targetPath);
        if (directory is null)
        {
            throw new InvalidDataException($"Document target has no directory: {targetPath}");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            CassetteDocumentCopyResult result = await CopyAsync(
                source,
                temporaryPath,
                maxBytes,
                cancellationToken);
            File.Move(temporaryPath, targetPath, overwrite);
            return result;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task<CassetteDocumentCopyResult> CopyAsync(
        Stream source,
        string temporaryPath,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        await using FileStream output = new(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
        long length = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            length += read;
            if (length > maxBytes)
            {
                throw new InvalidDataException($"Document exceeds the {maxBytes}-byte upload limit.");
            }

            hash.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (length == 0)
        {
            throw new InvalidDataException("Document content must not be empty.");
        }

        await output.FlushAsync(cancellationToken);
        output.Flush(flushToDisk: true);
        return new CassetteDocumentCopyResult(
            length,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }
}
