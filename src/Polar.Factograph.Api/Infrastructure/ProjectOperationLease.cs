namespace Polar.Factograph.Api.Infrastructure;

internal sealed class ProjectOperationLease(SemaphoreSlim gate) : IAsyncDisposable
{
    private SemaphoreSlim? _gate = gate;

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _gate, null)?.Release();
        return ValueTask.CompletedTask;
    }
}
