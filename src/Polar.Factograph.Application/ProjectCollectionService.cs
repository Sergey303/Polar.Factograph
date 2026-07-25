using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed class ProjectCollectionService
{
    private readonly IProjectRdfStore _rdfStore;
    private readonly ProjectCollectionMembershipReader _memberships;
    private readonly ProjectCollectionItemEnricher _enricher;

    public ProjectCollectionService(
        IProjectRdfStore rdfStore,
        IProjectSearchStore searchStore,
        OntologyCatalog? ontology = null)
    {
        ArgumentNullException.ThrowIfNull(rdfStore);
        ArgumentNullException.ThrowIfNull(searchStore);

        _rdfStore = rdfStore;
        _memberships = new ProjectCollectionMembershipReader(rdfStore);
        ProjectResourceSummaryReader summaries = new(rdfStore, searchStore, ontology);
        _enricher = new ProjectCollectionItemEnricher(summaries);
    }

    public async Task<ProjectCollectionContents?> GetAsync(
        string collectionId,
        IReadOnlySet<string> allowedCassetteIds,
        int limit = 100,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ProjectCollectionRequestRules.Validate(
            collectionId,
            allowedCassetteIds,
            limit,
            preferredLanguage);
        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(allowedCassetteIds);
        ResourceHead? collection = await _rdfStore.GetResourceHeadAsync(collectionId, cancellationToken);
        if (collection is null || collection.IsDeleted || !cassetteIds.Contains(collection.SourceCassetteId))
        {
            return null;
        }

        IReadOnlyList<ProjectCollectionItemReference> references =
            await _memberships.ReadAsync(collectionId, cassetteIds, cancellationToken);
        IReadOnlyList<ProjectCollectionItem> items = await _enricher.EnrichAsync(
            references,
            cassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
        return new ProjectCollectionContents(collectionId, items);
    }
}
