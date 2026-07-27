using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectFogMutationRunner
{
    private readonly IFogSourceScanner _sourceScanner;
    private readonly IdentityFogSourceResolver? _fogSourceResolver;
    private readonly ProjectOperationGate _operationGate;
    private readonly ProjectIndexDirtyMarker _dirtyMarker;
    private readonly ProjectWriteIndexRefresher _indexRefresher;

    public ProjectFogMutationRunner(
        IFogSourceScanner sourceScanner,
        IdentityFogSourceResolver fogSourceResolver,
        ProjectOperationGate operationGate,
        ProjectIndexDirtyMarker dirtyMarker,
        ProjectWriteIndexRefresher indexRefresher)
    {
        _sourceScanner = sourceScanner;
        _fogSourceResolver = fogSourceResolver;
        _operationGate = operationGate;
        _dirtyMarker = dirtyMarker;
        _indexRefresher = indexRefresher;
    }

    public ProjectFogMutationRunner(
        IFogSourceScanner sourceScanner,
        ProjectOperationGate operationGate,
        ProjectIndexDirtyMarker dirtyMarker,
        ProjectWriteIndexRefresher indexRefresher)
    {
        _sourceScanner = sourceScanner;
        _operationGate = operationGate;
        _dirtyMarker = dirtyMarker;
        _indexRefresher = indexRefresher;
    }

    public async Task<ProjectFogMutationOutcome<T>> RunAsync<T>(
        ProjectDefinition project,
        string userId,
        string cassetteId,
        Func<FogSourceDescriptor, CancellationToken, Task<T>> write,
        CancellationToken cancellationToken = default,
        Func<CancellationToken, Task>? beforeDirty = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);
        ArgumentNullException.ThrowIfNull(write);

        await using IAsyncDisposable lease = await _operationGate.AcquireAsync(
            project.Index.Path,
            cancellationToken);
        await _indexRefresher.EnsureCleanAsync(project, cancellationToken);
        if (beforeDirty is not null)
        {
            await beforeDirty(cancellationToken);
        }

        IReadOnlyList<FogSourceDescriptor> sources = await _sourceScanner.ScanAsync(
            project,
            cancellationToken);
        FogSourceDescriptor source = _fogSourceResolver is null
            ? FogWritableSourceSelector.Select(sources, cassetteId)
            : _fogSourceResolver.Resolve(project, sources, userId, cassetteId);

        _dirtyMarker.Mark(project.Index.Path);
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

        ProjectIndexRefreshOutcome refresh = await _indexRefresher.RefreshAsync(project);
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
            _dirtyMarker.Clear(indexRoot);
        }
        catch
        {
            // A stale marker is safer than hiding the original write failure.
        }
    }
}
