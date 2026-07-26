namespace Polar.Factograph.Api.Previews;

public sealed class PreviewWorkerRuntimeState
{
    private readonly object sync = new();
    private PreviewWorkerRuntimeSnapshot snapshot = new(
        PreviewWorkerStates.Starting,
        Enabled: false,
        StartedAtUtc: null,
        StoppedAtUtc: null,
        LastCycleStartedAtUtc: null,
        LastCycleCompletedAtUtc: null,
        LastSuccessAtUtc: null,
        LastFailureAtUtc: null,
        LastHandled: 0,
        TotalHandled: 0,
        ConsecutiveFailures: 0,
        LastFailureCode: null);

    public PreviewWorkerRuntimeSnapshot Read()
    {
        lock (sync)
        {
            return snapshot;
        }
    }

    public void MarkDisabled(DateTimeOffset now)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = PreviewWorkerStates.Disabled,
                Enabled = false,
                StoppedAtUtc = now
            };
        }
    }

    public void MarkStarted(DateTimeOffset now)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = PreviewWorkerStates.Starting,
                Enabled = true,
                StartedAtUtc = now,
                StoppedAtUtc = null
            };
        }
    }

    public void MarkCycleStarted(DateTimeOffset now)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = PreviewWorkerStates.Working,
                LastCycleStartedAtUtc = now
            };
        }
    }

    public void MarkCycleCompleted(DateTimeOffset now, int handled)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(handled);
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = PreviewWorkerStates.Idle,
                LastCycleCompletedAtUtc = now,
                LastSuccessAtUtc = now,
                LastHandled = handled,
                TotalHandled = snapshot.TotalHandled + handled,
                ConsecutiveFailures = 0,
                LastFailureCode = null
            };
        }
    }

    public void MarkCycleFailure(DateTimeOffset now)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = PreviewWorkerStates.Degraded,
                LastFailureAtUtc = now,
                LastHandled = 0,
                ConsecutiveFailures = snapshot.ConsecutiveFailures + 1,
                LastFailureCode = PreviewWorkerFailureCodes.CycleFailed
            };
        }
    }

    public void MarkStopped(DateTimeOffset now)
    {
        lock (sync)
        {
            snapshot = snapshot with
            {
                State = snapshot.Enabled
                    ? PreviewWorkerStates.Stopped
                    : PreviewWorkerStates.Disabled,
                StoppedAtUtc = now
            };
        }
    }
}
