namespace Polar.Factograph.Fog;

internal sealed class FogWriteLease : IAsyncDisposable
{
    private readonly FileStream _lockFile;

    private FogWriteLease(FileStream lockFile)
    {
        _lockFile = lockFile;
    }

    public static FogWriteLease Acquire(string fogPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fogPath);
        string lockPath = fogPath + ".write.lock";

        try
        {
            FileStream stream = new(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 1,
                FileOptions.DeleteOnClose);
            return new FogWriteLease(stream);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException(
                $"Fog file is already being written: {fogPath}",
                exception);
        }
    }

    public ValueTask DisposeAsync() => _lockFile.DisposeAsync();
}
