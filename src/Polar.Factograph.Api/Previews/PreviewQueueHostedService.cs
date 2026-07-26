using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Previews;

public sealed class PreviewQueueHostedService(
    ProjectPathResolver pathResolver,
    ProjectConfigurationLoader configurationLoader,
    PreviewWorkerCycle worker,
    IOptions<PreviewWorkerOptions> options,
    ILogger<PreviewQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PreviewWorkerOptions settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Preview worker is disabled.");
            return;
        }

        logger.LogInformation("Preview worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ProjectDefinition project = await configurationLoader.LoadAsync(
                    pathResolver.GetRequiredPath(),
                    stoppingToken);
                int handled = await worker.RunAsync(project, stoppingToken);
                if (handled == 0)
                {
                    await DelayAsync(settings, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Preview worker cycle failed.");
                await DelayAsync(settings, stoppingToken);
            }
        }
    }

    private static Task DelayAsync(
        PreviewWorkerOptions settings,
        CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), cancellationToken);
}
