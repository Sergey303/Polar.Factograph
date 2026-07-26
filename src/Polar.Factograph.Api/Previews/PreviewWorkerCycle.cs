using Microsoft.Extensions.Options;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

public sealed class PreviewWorkerCycle(
    FileSystemCassettePreviewQueueProcessor processor,
    ICassettePreviewRenderer renderer,
    IOptions<PreviewWorkerOptions> options)
{
    public async Task<int> RunAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        PreviewWorkerOptions settings = options.Value;
        CassettePreviewProcessingOptions processing = new()
        {
            MaxAttempts = settings.MaxAttempts,
            RetryDelay = TimeSpan.FromSeconds(settings.RetryDelaySeconds),
            LeaseTimeout = TimeSpan.FromSeconds(settings.LeaseTimeoutSeconds)
        };
        CassetteDefinition[] cassettes = project.Cassettes
            .Where(cassette => cassette.Enabled)
            .OrderBy(cassette => cassette.Id, StringComparer.Ordinal)
            .ToArray();
        int handled = 0;
        bool foundWork;
        do
        {
            foundWork = false;
            foreach (CassetteDefinition cassette in cassettes)
            {
                if (handled >= settings.MaxItemsPerCycle)
                {
                    return handled;
                }

                CassettePreviewProcessResult result = await processor.ProcessNextAsync(
                    cassette,
                    renderer,
                    processing,
                    cancellationToken);
                if (result.State == PreviewProcessStates.Empty)
                {
                    continue;
                }

                handled++;
                foundWork = true;
            }
        }
        while (foundWork && handled < settings.MaxItemsPerCycle);

        return handled;
    }
}
