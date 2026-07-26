using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectFogMutationRunner(
    IFogSourceScanner sourceScanner,
    ProjectOperationGate operationGate,
    ProjectIndexDirtyMarker dirtyMarker,
    ProjectWriteIndexRefresher indexRefresher)
{
    public async Task<ProjectFogMutationOutcome<T>> RunAsync<T>(
        ProjectDefinition project,
        string cassetteId,
        Func<FogSourceDescriptor, CancellationToken, Task<T>> write,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);
        ArgumentNullException.ThrowIfNull(write);

        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            project.Index.Path,
            cancellationToken);
        await indexRefresher.EnsureCleanAsync(project, cancellationToken);
        IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
            project,
            cancellationToken);
        FogSourceDescriptor source = FogWritableSourceSelector.Select(sources, cassetteId);

        dirtyMarker.Mark(project.Index.Path);
        T written;
        try
        {
            written = await write(source, cancellationToken);
        }
        catch
        {
            TryClear(project.Index.Path);
            throw;
        }

        ProjectIndexRefreshOutcome refresh = await indexRefresher.RefreshAsync(project);
        return new ProjectFogMutationOutcome<T>(
            written,
            cassetteId,
            refresh.IndexReady,
            refresh.GenerationId);
    }

    private void TryClear(string indexRoot)
    {
        try
        {
            dirtyMarker.Clear(indexRoot);
        }
        catch
        {
            // A stale marker is safer than hiding the original write failure.
        }
    }
}
