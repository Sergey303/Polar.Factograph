using System.Runtime.CompilerServices;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal sealed class ObjectTargetStoreStub(ResourceHead? head) : IProjectRdfStore
{
    public ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(
            string.Equals(head?.ResourceId, resourceId, StringComparison.Ordinal)
                ? head
                : null);

    public async IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield break;
    }

    public Task RebuildAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
