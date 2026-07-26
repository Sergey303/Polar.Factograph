namespace Polar.Factograph.Api.Previews;

public static class PreviewWorkerHealthEvaluator
{
    public static PreviewWorkerHealth Evaluate(
        PreviewWorkerRuntimeSnapshot snapshot,
        PreviewWorkerOptions options,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        if (!snapshot.Enabled)
        {
            return new PreviewWorkerHealth(
                PreviewWorkerStates.Disabled,
                Enabled: false,
                Degraded: false);
        }

        if (snapshot.State is PreviewWorkerStates.Degraded or PreviewWorkerStates.Stopped)
        {
            return new PreviewWorkerHealth(snapshot.State, Enabled: true, Degraded: true);
        }

        if (IsStale(snapshot, options, now))
        {
            return new PreviewWorkerHealth(
                PreviewWorkerStates.Unresponsive,
                Enabled: true,
                Degraded: true);
        }

        return new PreviewWorkerHealth(snapshot.State, Enabled: true, Degraded: false);
    }

    private static bool IsStale(
        PreviewWorkerRuntimeSnapshot snapshot,
        PreviewWorkerOptions options,
        DateTimeOffset now)
    {
        if (snapshot.State == PreviewWorkerStates.Working &&
            snapshot.LastCycleStartedAtUtc is { } cycleStarted)
        {
            long maximumSeconds = Math.Clamp(
                (long)options.RenderTimeoutSeconds * options.MaxItemsPerCycle + 60,
                60,
                86_400);
            return now - cycleStarted > TimeSpan.FromSeconds(maximumSeconds);
        }

        if (snapshot.State is PreviewWorkerStates.Starting or PreviewWorkerStates.Idle)
        {
            DateTimeOffset? activity = snapshot.LastCycleCompletedAtUtc ?? snapshot.StartedAtUtc;
            long maximumSeconds = Math.Clamp(
                (long)options.PollIntervalSeconds * 4 + 30,
                30,
                3_600);
            return activity is { } value && now - value > TimeSpan.FromSeconds(maximumSeconds);
        }

        return false;
    }
}
