using System.Runtime.CompilerServices;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Application.Tests;

internal sealed class CollectionRdfStoreStub : IProjectRdfStore
{
    private readonly Dictionary<string, ResourceHead> _heads;
    private readonly TripleRow[] _triples;

    public CollectionRdfStoreStub(
        IEnumerable<ResourceHead> heads,
        IEnumerable<TripleRow> triples)
    {
        _heads = heads.ToDictionary(head => head.ResourceId, StringComparer.Ordinal);
        _triples = triples.ToArray();
    }

    public int FindCalls { get; private set; }

    public ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(
            _heads.TryGetValue(resourceId, out ResourceHead? head) ? head : null);
    }

    public async IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        FindCalls++;
        await Task.Yield();

        foreach (TripleRow triple in _triples)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!allowedCassetteIds.Contains(triple.SourceCassetteId) || !Matches(triple, pattern))
            {
                continue;
            }

            yield return triple;
        }
    }

    public Task RebuildAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private static bool Matches(TripleRow triple, TriplePattern pattern) =>
        (pattern.Subject is null || string.Equals(pattern.Subject, triple.Subject, StringComparison.Ordinal)) &&
        (pattern.Predicate is null || string.Equals(pattern.Predicate, triple.Predicate, StringComparison.Ordinal)) &&
        (pattern.ObjectKind is null || pattern.ObjectKind == triple.ObjectKind) &&
        (pattern.ObjectValue is null || string.Equals(pattern.ObjectValue, triple.ObjectValue, StringComparison.Ordinal));
}
