namespace Polar.Factograph.Fog;

internal static class CassetteDocumentAtomicFileWriter
{
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
            CassetteDocumentCopyResult result = await CassetteDocumentStreamCopier.CopyAsync(
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
}
