using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Previews;

public sealed class PreviewQueueHostedService(
    ProjectPathResolver pathResolver,
    ProjectConfigurationLoader configurationLoader,
    PreviewWorkerCycle worker,
    PreviewWorkerRuntimeState runtime,
    IOptions<PreviewWorkerOptions> options,
    ILogger<PreviewQueueHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PreviewWorkerOptions settings = options.Value;
        if (!settings.Enabled)
        {
            runtime.MarkDisabled(DateTimeOffset.UtcNow);
            logger.LogInformation("Preview worker is disabled.");
            return;
        }

        runtime.MarkStarted(DateTimeOffset.UtcNow);
        logger.LogInformation("Preview worker started.");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                runtime.MarkCycleStarted(DateTimeOffset.UtcNow);
                try
                {
                    ProjectDefinition project = await configurationLoader.LoadAsync(
                        pathResolver.GetRequiredPath(),
                        stoppingToken);
                    int handled = await worker.RunAsync(project, stoppingToken);
                    runtime.MarkCycleCompleted(DateTimeOffset.UtcNow, handled);
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
                    runtime.MarkFailure(DateTimeOffset.UtcNow, "cycle-failed");
                    logger.LogError(exception, "Preview worker cycle failed.");
                    await DelayAsync(settings, stoppingToken);
                }
            }
        }
        finally
        {
            runtime.MarkStopped(DateTimeOffset.UtcNow);
        }
    }

    private static Task DelayAsync(
        PreviewWorkerOptions settings,
        CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromSeconds(settings.PollIntervalSeconds), cancellationToken);
}
