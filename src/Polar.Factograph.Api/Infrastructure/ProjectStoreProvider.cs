using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectStoreProvider(ProjectIndexDirtyMarker dirtyMarker) : IDisposable
{
    private readonly object _sync = new();
    private readonly List<PolarDbTypedProjectStore> _retired = new();
    private PolarDbTypedProjectStore? _current;
    private string? _indexRoot;

    public PolarDbTypedProjectStore GetCurrent(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        string fullRoot = Path.GetFullPath(indexRoot);
        if (dirtyMarker.Exists(fullRoot))
        {
            throw new ProjectRuntimeUnavailableException(
                "The project index is waiting for a successful rebuild.");
        }

        lock (_sync)
        {
            string generationPath = FileSystemIndexGeneration.GetCurrentGenerationPath(fullRoot)
                ?? throw new ProjectRuntimeUnavailableException(
                    $"The project index has no CURRENT generation: {fullRoot}");

            if (_current is not null &&
                string.Equals(_indexRoot, fullRoot, StringComparison.Ordinal) &&
                string.Equals(_current.GenerationPath, generationPath, StringComparison.Ordinal))
            {
                return _current;
            }

            PolarDbTypedProjectStore opened;
            try
            {
                opened = PolarDbTypedProjectStore.OpenCurrent(fullRoot);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException)
            {
                throw new ProjectRuntimeUnavailableException(
                    $"The current project index cannot be opened: {fullRoot}",
                    exception);
            }

            if (_current is not null)
            {
                _retired.Add(_current);
            }

            _current = opened;
            _indexRoot = fullRoot;
            return opened;
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _current?.Dispose();
            _current = null;

            foreach (PolarDbTypedProjectStore store in _retired)
            {
                store.Dispose();
            }

            _retired.Clear();
        }
    }
}
