using Microsoft.Extensions.Options;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Previews;

public sealed class ExternalProcessPreviewRenderer(
    IOptions<PreviewWorkerOptions> options,
    ILogger<ExternalProcessPreviewRenderer> logger) : ICassettePreviewRenderer
{
    public async Task RenderAsync(
        CassetteDefinition cassette,
        CassettePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        PreviewWorkerOptions settings = options.Value;
        if (!settings.IsValid() || string.IsNullOrWhiteSpace(settings.Executable))
        {
            throw new PreviewRenderingException(
                "Preview rendering is not configured.",
                retryable: false);
        }

        using PreviewOutputPlan plan = PreviewOutputPlan.Create(cassette, request, settings);
        if (!await PreviewOriginalVersion.MatchesAsync(
                plan.OriginalPath,
                request,
                cancellationToken))
        {
            logger.LogInformation(
                "Skipping superseded preview request {RequestId} for {DocumentUri}.",
                request.RequestId,
                request.DocumentUri);
            return;
        }

        ExternalPreviewProcessResult result = await ExternalPreviewProcess.RunAsync(
            settings,
            plan,
            cancellationToken);
        if (result.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(result.StandardError)
                ? result.StandardOutput
                : result.StandardError;
            string message = string.IsNullOrWhiteSpace(detail)
                ? $"Preview renderer exited with code {result.ExitCode}."
                : $"Preview renderer exited with code {result.ExitCode}: {detail.Trim()}";
            throw new PreviewRenderingException(
                message,
                retryable: result.ExitCode != 64);
        }

        foreach (PreviewOutputFile output in plan.Outputs)
        {
            if (!File.Exists(output.TemporaryPath) || new FileInfo(output.TemporaryPath).Length == 0)
            {
                throw new PreviewRenderingException(
                    $"Preview renderer did not produce the {output.Variant} output.",
                    retryable: true);
            }
        }

        if (!await PreviewOriginalVersion.MatchesAsync(
                plan.OriginalPath,
                request,
                cancellationToken))
        {
            logger.LogInformation(
                "Discarding previews from superseded request {RequestId}.",
                request.RequestId);
            return;
        }

        plan.Publish();
    }
}
