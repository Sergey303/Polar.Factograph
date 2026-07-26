using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public static class PreviewProcessStates
{
    public const string Empty = "empty";
    public const string Completed = "completed";
    public const string Retried = "retried";
    public const string Failed = "failed";
    public const string Invalid = "invalid";
}

public sealed record CassettePreviewProcessResult(
    string State,
    string? RequestId,
    int Attempt,
    string? Error);

public sealed record CassettePreviewProcessingOptions
{
    public int MaxAttempts { get; init; } = 3;
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan LeaseTimeout { get; init; } = TimeSpan.FromMinutes(30);
}

public interface ICassettePreviewRenderer
{
    Task RenderAsync(
        CassetteDefinition cassette,
        CassettePreviewRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PreviewRenderingException(
    string message,
    bool retryable,
    Exception? innerException = null) : Exception(message, innerException)
{
    public bool Retryable { get; } = retryable;
}