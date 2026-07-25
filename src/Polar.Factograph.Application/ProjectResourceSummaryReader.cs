using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectResourceSummaryReader
{
    private readonly IProjectRdfStore _rdfStore;
    private readonly IProjectSearchStore _searchStore;
    private readonly OntologyCatalog? _ontology;
    private readonly ProjectResourceTypeReader _typeReader;

    public ProjectResourceSummaryReader(
        IProjectRdfStore rdfStore,
        IProjectSearchStore searchStore,
        OntologyCatalog? ontology)
    {
        _rdfStore = rdfStore;
        _searchStore = searchStore;
        _ontology = ontology;
        _typeReader = new ProjectResourceTypeReader(rdfStore);
    }

    public async Task<ProjectResourceSummary?> ReadAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        ResourceHead? head = await GetVisibleHeadAsync(resourceId, cassetteIds, cancellationToken);
        if (head is null) return null;

        IReadOnlyList<NameSearchHit> names = await _searchStore.FindNamesByResourceAsync(
            resourceId,
            cassetteIds,
            cancellationToken);
        string displayName = ProjectSearchRules.SelectDisplayName(
            names,
            preferredLanguage,
            resourceId);
        return await BuildAsync(head, displayName, cassetteIds, preferredLanguage, cancellationToken);
    }

    public async Task<ProjectResourceSummary?> ReadAsync(
        string resourceId,
        string displayName,
        IReadOnlySet<string> cassetteIds,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        ResourceHead? head = await GetVisibleHeadAsync(resourceId, cassetteIds, cancellationToken);
        return head is null
            ? null
            : await BuildAsync(head, displayName, cassetteIds, preferredLanguage, cancellationToken);
    }

    private async Task<ProjectResourceSummary> BuildAsync(
        ResourceHead head,
        string displayName,
        IReadOnlySet<string> cassetteIds,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        string? type = await _typeReader.ReadAsync(head.ResourceId, cassetteIds, cancellationToken);
        return new ProjectResourceSummary(
            head.ResourceId,
            displayName,
            type,
            type is null ? null : _ontology?.LabelOf(type, preferredLanguage) ?? type,
            head.SourceCassetteId);
    }

    private async ValueTask<ResourceHead?> GetVisibleHeadAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        ResourceHead? head = await _rdfStore.GetResourceHeadAsync(resourceId, cancellationToken);
        return head is null || head.IsDeleted || !cassetteIds.Contains(head.SourceCassetteId)
            ? null
            : head;
    }
}
