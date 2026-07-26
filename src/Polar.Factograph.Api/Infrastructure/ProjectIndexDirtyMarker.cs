namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectIndexDirtyMarker(TimeProvider? timeProvider = null)
{
    private const string MarkerName = "DIRTY";
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public bool Exists(string indexRoot) => File.Exists(GetPath(indexRoot));

    public void Mark(string indexRoot)
    {
        string root = Path.GetFullPath(indexRoot);
        Directory.CreateDirectory(root);
        File.WriteAllText(
            Path.Combine(root, MarkerName),
            _timeProvider.GetUtcNow().ToString("O"));
    }

    public void Clear(string indexRoot)
    {
        string path = GetPath(indexRoot);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string GetPath(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        return Path.Combine(Path.GetFullPath(indexRoot), MarkerName);
    }
}
