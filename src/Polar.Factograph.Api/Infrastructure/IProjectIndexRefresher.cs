using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Infrastructure;

public interface IProjectIndexRefresher
{
    Task<ProjectIndexRebuildResult> RebuildAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default);
}
