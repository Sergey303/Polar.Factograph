using System.Collections.Concurrent;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectOperationGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _gates =
        new(StringComparer.Ordinal);

    public async ValueTask<IAsyncDisposable> AcquireAsync(
        string indexRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        string key = Path.GetFullPath(indexRoot);
        SemaphoreSlim gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        return new ProjectOperationLease(gate);
    }
}
