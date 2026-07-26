using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceWriteCoordinator(
    IFogSourceScanner sourceScanner,
    IFogResourceWriter resourceWriter,
    ProjectWriteCassetteResolver cassetteResolver,
    ProjectOperationGate operationGate,
    ProjectIndexDirtyMarker dirtyMarker,
    ProjectWriteIndexRefresher indexRefresher)
{
    public async Task<ProjectResourceWriteOutcome> WriteAsync(
        ProjectAccessContext context,
        FogResourceWriteRequest request,
        string? requestedCassetteId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            requestedCassetteId);

        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            context.Project.Index.Path,
            cancellationToken);
        await indexRefresher.EnsureCleanAsync(
            context.Project,
            cancellationToken);
        IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
            context.Project,
            cancellationToken);
        FogSourceDescriptor source = FogWritableSourceSelector.Select(
            sources,
            cassetteId);

        dirtyMarker.Mark(context.Project.Index.Path);
        FogResourceWriteResult written;
        try
        {
            written = await resourceWriter.AppendAsync(
                source,
                request,
                cancellationToken);
        }
        catch
        {
            ClearAfterFailedWrite(context.Project.Index.Path);
            throw;
        }

        return await indexRefresher.RefreshAsync(
            context.Project,
            written,
            cassetteId);
    }

    private void ClearAfterFailedWrite(string indexRoot)
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
