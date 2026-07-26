using Polar.Factograph.Api.Previews;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class PreviewWorkerRuntimeStateTests
{
    [Fact]
    public void CompletedCycle_RecordsTotalsAndClearsFailure()
    {
        DateTimeOffset started = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
        PreviewWorkerRuntimeState runtime = new();
        runtime.MarkStarted(started);
        runtime.MarkFailure(started.AddSeconds(1), "cycle-failed");
        runtime.MarkCycleStarted(started.AddSeconds(2));
        runtime.MarkCycleCompleted(started.AddSeconds(3), handled: 4);

        PreviewWorkerRuntimeSnapshot snapshot = runtime.Read();

        Assert.Equal(PreviewWorkerStates.Idle, snapshot.State);
        Assert.True(snapshot.Enabled);
        Assert.Equal(4, snapshot.LastHandled);
        Assert.Equal(4, snapshot.TotalHandled);
        Assert.Equal(0, snapshot.ConsecutiveFailures);
        Assert.Null(snapshot.LastFailureCode);
        Assert.Equal(started.AddSeconds(3), snapshot.LastSuccessAtUtc);
    }

    [Fact]
    public void FailedCycle_ExposesOnlyStableFailureCode()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PreviewWorkerRuntimeState runtime = new();
        runtime.MarkStarted(now);
        runtime.MarkCycleStarted(now);
        runtime.MarkFailure(now.AddSeconds(1), "cycle-failed");

        PreviewWorkerRuntimeSnapshot snapshot = runtime.Read();
        PreviewWorkerHealth health = PreviewWorkerHealthEvaluator.Evaluate(
            snapshot,
            EnabledOptions(),
            now.AddSeconds(2));

        Assert.Equal("cycle-failed", snapshot.LastFailureCode);
        Assert.Equal(1, snapshot.ConsecutiveFailures);
        Assert.Equal(PreviewWorkerStates.Degraded, health.State);
        Assert.True(health.Degraded);
    }

    [Fact]
    public void Evaluate_WhenIdleStopsCycling_ReportsUnresponsive()
    {
        DateTimeOffset cycle = new(2026, 7, 26, 12, 0, 0, TimeSpan.Zero);
        PreviewWorkerRuntimeState runtime = new();
        runtime.MarkStarted(cycle);
        runtime.MarkCycleStarted(cycle);
        runtime.MarkCycleCompleted(cycle, handled: 0);

        PreviewWorkerHealth health = PreviewWorkerHealthEvaluator.Evaluate(
            runtime.Read(),
            EnabledOptions(),
            cycle.AddMinutes(2));

        Assert.Equal(PreviewWorkerStates.Unresponsive, health.State);
        Assert.True(health.Degraded);
    }

    [Fact]
    public void Evaluate_WhenFeatureDisabled_RemainsHealthy()
    {
        PreviewWorkerHealth health = PreviewWorkerHealthEvaluator.Evaluate(
            new PreviewWorkerRuntimeState().Read(),
            new PreviewWorkerOptions { Enabled = false },
            DateTimeOffset.UtcNow);

        Assert.Equal(PreviewWorkerStates.Disabled, health.State);
        Assert.False(health.Enabled);
        Assert.False(health.Degraded);
    }

    private static PreviewWorkerOptions EnabledOptions() => new()
    {
        Enabled = true,
        Executable = "renderer",
        PollIntervalSeconds = 5,
        RenderTimeoutSeconds = 30,
        MaxItemsPerCycle = 2
    };
}
