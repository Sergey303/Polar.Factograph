namespace Polar.Factograph.Fog;

internal sealed class CassetteDocumentWriteLease : IAsyncDisposable
{
    private readonly FileStream _lockFile;

    private CassetteDocumentWriteLease(FileStream lockFile)
    {
        _lockFile = lockFile;
    }

    public static CassetteDocumentWriteLease Acquire(string cassettePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cassettePath);
        string root = Path.GetFullPath(cassettePath);
        Directory.CreateDirectory(root);
        string lockPath = Path.Combine(root, ".documents.write.lock");

        try
        {
            FileStream stream = new(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
            return new CassetteDocumentWriteLease(stream);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"Cassette documents are already being written: {root}",
                exception);
        }
    }

    public ValueTask DisposeAsync() => _lockFile.DisposeAsync();
}
