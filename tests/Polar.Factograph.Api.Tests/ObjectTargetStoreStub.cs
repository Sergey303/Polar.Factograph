using System.Runtime.CompilerServices;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal sealed class ObjectTargetStoreStub(
    ResourceHead? head,
    IReadOnlyList<string>? types = null) : IProjectRdfStore
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

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
        if (head is null || !allowedCassetteIds.Contains(head.SourceCassetteId) ||
            !string.Equals(pattern.Subject, head.ResourceId, StringComparison.Ordinal) ||
            !string.Equals(pattern.Predicate, RdfType, StringComparison.Ordinal))
        {
            yield break;
        }

        foreach (string type in types ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new TripleRow(
                Guid.NewGuid(),
                head.ResourceId,
                RdfType,
                TripleObjectKind.Iri,
                type,
                null,
                null,
                head.CurrentSourceRecordId,
                head.SourceCassetteId,
                head.SourceFogPath,
                head.ModifiedAt);
        }
    }

    public Task RebuildAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
