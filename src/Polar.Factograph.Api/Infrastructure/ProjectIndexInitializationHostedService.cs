using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectIndexInitializationHostedService(
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    ProjectIndexCoordinator indexCoordinator,
    ProjectIndexDirtyMarker dirtyMarker,
    ILogger<ProjectIndexInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string projectPath = projectPathResolver.GetRequiredPath();
        ProjectDefinition project = await projectLoader.LoadAsync(
            projectPath,
            cancellationToken);
        string indexRoot = Path.GetFullPath(project.Index.Path);
        bool dirty = dirtyMarker.Exists(indexRoot);
        string? currentGeneration = FileSystemIndexGeneration.GetCurrentGenerationPath(indexRoot);

        if (!dirty && currentGeneration is not null)
        {
            logger.LogInformation(
                "Using project index generation {GenerationPath}.",
                currentGeneration);
            return;
        }

        logger.LogInformation(
            dirty
                ? "The project index is marked DIRTY and will be rebuilt before requests are accepted."
                : "The project index has no CURRENT generation and will be built before requests are accepted.");

        ProjectIndexRebuildResult result = await indexCoordinator.RebuildAsync(
            project,
            cancellationToken);

        logger.LogInformation(
            "Project index generation {GenerationId} is ready from {SourceFiles} Fog sources.",
            result.GenerationId,
            result.SourceFiles);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
