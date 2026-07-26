namespace Polar.Factograph.Fog;

internal sealed class CassettePreviewLeaseHeartbeat : IAsyncDisposable
{
    private readonly CancellationTokenSource stop = new();
    private readonly Task heartbeat;

    private CassettePreviewLeaseHeartbeat(string processingPath, TimeSpan leaseTimeout)
    {
        TimeSpan interval = TimeSpan.FromTicks(Math.Clamp(
            leaseTimeout.Ticks / 3,
            TimeSpan.FromMilliseconds(100).Ticks,
            TimeSpan.FromMinutes(1).Ticks));
        heartbeat = RunAsync(processingPath, interval, stop.Token);
    }

    public static CassettePreviewLeaseHeartbeat Start(
        string processingPath,
        TimeSpan leaseTimeout) =>
        new(processingPath, leaseTimeout);

    public async ValueTask DisposeAsync()
    {
        await stop.CancelAsync();
        try
        {
            await heartbeat;
        }
        finally
        {
            stop.Dispose();
        }
    }

    private static async Task RunAsync(
        string processingPath,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    File.SetLastWriteTimeUtc(processingPath, DateTime.UtcNow);
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException)
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}