namespace Polar.Factograph.Api.Previews;

public static class PreviewWorkerStates
{
    public const string Disabled = "disabled";
    public const string Starting = "starting";
    public const string Working = "working";
    public const string Idle = "idle";
    public const string Degraded = "degraded";
    public const string Stopped = "stopped";
}

public sealed record PreviewWorkerRuntimeSnapshot(
    string State,
    bool Enabled,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? StoppedAtUtc,
    DateTimeOffset? LastCycleStartedAtUtc,
    DateTimeOffset? LastCycleCompletedAtUtc,
    DateTimeOffset? LastSuccessAtUtc,
    DateTimeOffset? LastFailureAtUtc,
    int LastHandled,
    long TotalHandled,
    int ConsecutiveFailures,
    string? LastFailureCode);

public sealed record PreviewWorkerHealth(
    string State,
    bool Enabled,
    bool Degraded);
