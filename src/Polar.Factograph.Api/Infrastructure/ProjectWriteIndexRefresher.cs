using Polar.Factograph.Domain;

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

    public async Task<ProjectIndexRefreshOutcome> RefreshAsync(
        ProjectDefinition project)
    {
        try
        {
            ProjectIndexRebuildResult rebuild = await indexCoordinator
                .RebuildUnderLeaseAsync(project, CancellationToken.None);
            return new ProjectIndexRefreshOutcome(
                IndexReady: true,
                rebuild.GenerationId);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Fog mutation succeeded, but index rebuild failed.");
            return new ProjectIndexRefreshOutcome(
                IndexReady: false,
                GenerationId: null);
        }
    }
}
