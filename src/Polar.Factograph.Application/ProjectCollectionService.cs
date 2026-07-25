using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed class ProjectCollectionService
{
    private const string InCollection = "http://fogid.net/o/in-collection";
    private const string CollectionItem = "http://fogid.net/o/collection-item";

    private readonly IProjectRdfStore _rdfStore;
    private readonly ProjectCollectionItemEnricher _enricher;

    public ProjectCollectionService(
        IProjectRdfStore rdfStore,
        IProjectSearchStore searchStore,
        OntologyCatalog? ontology = null)
    {
        ArgumentNullException.ThrowIfNull(rdfStore);
        ArgumentNullException.ThrowIfNull(searchStore);

        _rdfStore = rdfStore;
        ProjectResourceSummaryReader summaryReader = new(rdfStore, searchStore, ontology);
        _enricher = new ProjectCollectionItemEnricher(summaryReader);
    }

    public async Task<ProjectCollectionContents?> GetAsync(
        string collectionId,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 100,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        Validate(collectionId, allowedCassetteIds, limit, preferredLanguage);
        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(allowedCassetteIds);
        ResourceHead? collection = await _rdfStore.GetResourceHeadAsync(collectionId, cancellationToken);
        if (collection is null || collection.IsDeleted || !cassetteIds.Contains(collection.SourceCassetteId))
        {
            return null;
        }

        List<ProjectCollectionItemReference> references = new();
        await foreach (TripleRow membership in _rdfStore.FindAsync(
                           new TriplePattern(
                               Predicate: InCollection,
                               ObjectKind: TripleObjectKind.Iri,
                               ObjectValue: collectionId),
                           cassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            await ReadMembershipAsync(membership, cassetteIds, references, cancellationToken);
        }

        IReadOnlyList<ProjectCollectionItem> items = await _enricher.EnrichAsync(
            references
                .Distinct()
                .OrderBy(reference => reference.MembershipResourceId, StringComparer.Ordinal)
                .ThenBy(reference => reference.ResourceId, StringComparer.Ordinal),
            cassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
        return new ProjectCollectionContents(collectionId, items);
    }

    private async Task ReadMembershipAsync(
        TripleRow membership,
        IReadOnlySet<string> cassetteIds,
        ICollection<ProjectCollectionItemReference> references,
        CancellationToken cancellationToken)
    {
        await foreach (TripleRow item in _rdfStore.FindAsync(
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

    private static void Validate(
        string collectionId,
        IReadOnlySet<string> allowedCassetteIds,
        int limit,
        string preferredLanguage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionId);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);
        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(limit),
                limit,
                "Collection limit must be between 1 and 500.");
        }
    }
}
