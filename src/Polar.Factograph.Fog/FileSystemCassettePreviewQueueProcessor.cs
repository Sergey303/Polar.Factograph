using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemCassettePreviewQueueProcessor
{
    public async Task<CassettePreviewProcessResult> ProcessNextAsync(
        CassetteDefinition cassette,
        ICassettePreviewRenderer renderer,
        CassettePreviewProcessingOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        ArgumentNullException.ThrowIfNull(renderer);
        options ??= new CassettePreviewProcessingOptions();
        ValidateOptions(options);

        CassettePreviewQueuePaths paths = CassettePreviewQueuePaths.Create(cassette);
        paths.EnsureDirectories();
        CassettePreviewQueueFiles.RecoverStale(paths, options.LeaseTimeout);

        foreach (string queuedPath in CassettePreviewQueueFiles.EnumerateQueued(paths).ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CassettePreviewQueueFiles.TryClaim(queuedPath, paths, out string processingPath))
            {
                continue;
            }

            CassettePreviewRequest request;
            try
            {
                request = await CassettePreviewQueueJson.ReadAsync(
                    processingPath,
                    cancellationToken);
                CassettePreviewRequestValidator.Validate(cassette, request);
            }
            catch (OperationCanceledException)
            {
                CassettePreviewQueueFiles.MoveBack(processingPath, paths);
                throw;
            }
            catch (Exception exception) when (
                exception is JsonException or InvalidDataException)
            {
                string failedPath = CassettePreviewQueueFiles.MoveToFailed(
                    processingPath,
                    paths,
                    "invalid");
                CassettePreviewQueueFiles.WriteFailureNote(failedPath, exception.Message);
                return new CassettePreviewProcessResult(
                    PreviewProcessStates.Invalid,
                    null,
                    0,
                    exception.Message);
            }

            if (request.NotBeforeUtc is { } notBefore && notBefore > DateTimeOffset.UtcNow)
            {
                CassettePreviewQueueFiles.MoveBack(processingPath, paths);
                continue;
            }

            try
            {
                await using (CassettePreviewLeaseHeartbeat.Start(
                                 processingPath,
                                 options.LeaseTimeout))
                {
                    await renderer.RenderAsync(cassette, request, cancellationToken);
                }

                File.Delete(processingPath);
                return new CassettePreviewProcessResult(
                    PreviewProcessStates.Completed,
                    request.RequestId,
                    request.Attempt + 1,
                    null);
            }
            catch (OperationCanceledException)
            {
                CassettePreviewQueueFiles.MoveBack(processingPath, paths);
                throw;
            }
            catch (PreviewRenderingException exception)
            {
                return await HandleFailureAsync(
                    request,
                    processingPath,
                    paths,
                    options,
                    exception.Message,
                    exception.Retryable);
            }
            catch (Exception exception)
            {
                return await HandleFailureAsync(
                    request,
                    processingPath,
                    paths,
                    options,
                    exception.Message,
                    retryable: true);
            }
        }

        return new CassettePreviewProcessResult(
            PreviewProcessStates.Empty,
            null,
            0,
            null);
    }

    private static async Task<CassettePreviewProcessResult> HandleFailureAsync(
        CassettePreviewRequest request,
        string processingPath,
        CassettePreviewQueuePaths paths,
        CassettePreviewProcessingOptions options,
        string error,
        bool retryable)
    {
        int attempt = request.Attempt + 1;
        string safeError = error.Length <= 1000 ? error : error[..1000];
        bool shouldRetry = retryable && attempt < options.MaxAttempts;
        CassettePreviewRequest updated = request with
        {
            Attempt = attempt,
            LastError = safeError,
            NotBeforeUtc = shouldRetry
                ? DateTimeOffset.UtcNow.Add(options.RetryDelay)
                : null
        };
        await CassettePreviewQueueJson.ReplaceAsync(
            processingPath,
            updated,
            CancellationToken.None);

        if (shouldRetry)
        {
            CassettePreviewQueueFiles.MoveBack(processingPath, paths);
            return new CassettePreviewProcessResult(
                PreviewProcessStates.Retried,
                request.RequestId,
                attempt,
                safeError);
        }

        CassettePreviewQueueFiles.MoveToFailed(processingPath, paths);
        return new CassettePreviewProcessResult(
            PreviewProcessStates.Failed,
            request.RequestId,
            attempt,
            safeError);
    }

    private static void ValidateOptions(CassettePreviewProcessingOptions options)
    {
        if (options.MaxAttempts <= 0 ||
            options.RetryDelay < TimeSpan.Zero ||
            options.LeaseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Preview processing limits are invalid.");
        }
    }
}