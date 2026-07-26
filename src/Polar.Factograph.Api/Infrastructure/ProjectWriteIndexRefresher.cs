using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectWriteIndexRefresher(
    ProjectIndexCoordinator indexCoordinator,
    ProjectIndexDirtyMarker dirtyMarker,
    ILogger<ProjectWriteIndexRefresher> logger)
{
    public async Task EnsureCleanAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken)
    {
        if (dirtyMarker.Exists(project.Index.Path))
        {
            await indexCoordinator.RebuildUnderLeaseAsync(project, cancellationToken);
        }
    }

    public async Task<ProjectResourceWriteOutcome> RefreshAsync(
        ProjectDefinition project,
        FogResourceWriteResult written,
        string cassetteId)
    {
        try
        {
            ProjectIndexRebuildResult rebuild = await indexCoordinator
                .RebuildUnderLeaseAsync(project, CancellationToken.None);
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
