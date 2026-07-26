using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public static class PreviewQueueStates
{
    public const string Queued = "queued";
    public const string QueueFailed = "queue-failed";
}

public sealed record CassettePreviewQueueResult(
    string State,
    string? RequestId,
    DateTimeOffset? QueuedAtUtc)
{
    public static CassettePreviewQueueResult Queued(
        string requestId,
        DateTimeOffset queuedAtUtc) =>
        new(PreviewQueueStates.Queued, requestId, queuedAtUtc);

    public static CassettePreviewQueueResult Failed() =>
        new(PreviewQueueStates.QueueFailed, null, null);
}

public interface ICassettePreviewRequestWriter
{
    Task<CassettePreviewQueueResult> QueueAsync(
        CassetteDefinition cassette,
        CassetteDocumentWriteResult document,
        CancellationToken cancellationToken = default);
}
