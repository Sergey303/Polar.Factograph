using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectCollectionMembershipReader(IProjectRdfStore rdfStore)
{
    private const string InCollection = "http://fogid.net/o/in-collection";
    private const string CollectionItem = "http://fogid.net/o/collection-item";

    public async Task<IReadOnlyList<ProjectCollectionItemReference>> ReadAsync(
        string collectionId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        List<ProjectCollectionItemReference> references = new();
        await foreach (TripleRow membership in rdfStore.FindAsync(
                           new TriplePattern(
                               Predicate: InCollection,
                               ObjectKind: TripleObjectKind.Iri,
                               ObjectValue: collectionId),
                           cassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            await AddItemsAsync(membership, cassetteIds, references, cancellationToken);
        }

        return references
            .Distinct()
            .OrderBy(reference => reference.MembershipResourceId, StringComparer.Ordinal)
            .ThenBy(reference => reference.ResourceId, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task AddItemsAsync(
        TripleRow membership,
        IReadOnlySet<string> cassetteIds,
        ICollection<ProjectCollectionItemReference> references,
        CancellationToken cancellationToken)
    {
        await foreach (TripleRow item in rdfStore.FindAsync(
                           new TriplePattern(
                               Subject: membership.Subject,
                               Predicate: CollectionItem,
                               ObjectKind: TripleObjectKind.Iri),
                           cassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            references.Add(new ProjectCollectionItemReference(
                membership.Subject,
                item.ObjectValue,
                membership.SourceCassetteId));
        }
    }
}
