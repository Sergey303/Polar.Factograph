using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Tests;

internal sealed class StubProjectIndexRefresher(
    ProjectIndexRebuildResult? result = null,
    Exception? exception = null) : IProjectIndexRefresher
{
    public int CallCount { get; private set; }

    public Task<ProjectIndexRebuildResult> RebuildAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return exception is null
            ? Task.FromResult(result!)
            : Task.FromException<ProjectIndexRebuildResult>(exception);
    }
}
