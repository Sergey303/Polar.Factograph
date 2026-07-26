using Microsoft.Extensions.Options;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

public sealed class PreviewWorkerCycle(
    FileSystemCassettePreviewQueueProcessor processor,
    ICassettePreviewRenderer renderer,
    IOptions<PreviewWorkerOptions> options)
{
    private readonly object _cursorLock = new();
    private int _nextCassetteIndex;

    public async Task<int> RunAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        PreviewWorkerOptions settings = options.Value;
        CassetteDefinition[] cassettes = project.Cassettes
            .Where(cassette => cassette.Enabled)
            .OrderBy(cassette => cassette.Id, StringComparer.Ordinal)
            .ToArray();
        if (cassettes.Length == 0)
        {
            return 0;
        }

        CassettePreviewProcessingOptions processing = new()
        {
            MaxAttempts = settings.MaxAttempts,
            RetryDelay = TimeSpan.FromSeconds(settings.RetryDelaySeconds),
            LeaseTimeout = TimeSpan.FromSeconds(settings.LeaseTimeoutSeconds)
        };
        int handled = 0;
        int consecutiveEmpty = 0;
        while (handled < settings.MaxItemsPerCycle && consecutiveEmpty < cassettes.Length)
        {
            CassetteDefinition cassette = cassettes[TakeNextIndex(cassettes.Length)];
            CassettePreviewProcessResult result = await processor.ProcessNextAsync(
                cassette,
                renderer,
                processing,
                cancellationToken);
            if (result.State == PreviewProcessStates.Empty)
            {
                consecutiveEmpty++;
                continue;
            }

            handled++;
            consecutiveEmpty = 0;
        }

        return handled;
    }

    private int TakeNextIndex(int count)
    {
        lock (_cursorLock)
        {
            int index = _nextCassetteIndex % count;
            _nextCassetteIndex = (index + 1) % count;
            return index;
        }
    }
}
