using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceWriteCoordinator(
    IFogSourceScanner sourceScanner,
    IFogResourceWriter resourceWriter,
    ProjectWriteCassetteResolver cassetteResolver,
    ProjectOperationGate operationGate,
    ProjectIndexDirtyMarker dirtyMarker,
    ProjectIndexCoordinator indexCoordinator,
    ILogger<ProjectResourceWriteCoordinator> logger)
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
            TryClearDirty(context.Project.Index.Path);
            throw;
        }

        try
        {
            ProjectIndexRebuildResult rebuild = await indexCoordinator
                .RebuildUnderLeaseAsync(context.Project, CancellationToken.None);
            return CreateOutcome(written, cassetteId, rebuild.GenerationId, indexReady: true);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Fog resource {ResourceId} was written, but index rebuild failed.",
                written.ResourceId);
            return CreateOutcome(written, cassetteId, generationId: null, indexReady: false);
        }
    }

    private void TryClearDirty(string indexRoot)
    {
        try
        {
            dirtyMarker.Clear(indexRoot);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not clear unused project DIRTY marker.");
        }
    }

    private static ProjectResourceWriteOutcome CreateOutcome(
        FogResourceWriteResult written,
        string cassetteId,
        Guid? generationId,
        bool indexReady) => new(
        written.ResourceId,
        cassetteId,
        written.ModifiedAtUtc,
        indexReady,
        generationId);
}
