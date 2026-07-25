using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectResourceSummaryReader(
    IProjectRdfStore rdfStore,
    IProjectSearchStore searchStore,
    OntologyCatalog? ontology)
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

    public async Task<ProjectResourceSummary?> ReadAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        ResourceHead? head = await GetVisibleHeadAsync(resourceId, cassetteIds, cancellationToken);
        if (head is null) return null;

        IReadOnlyList<NameSearchHit> names = await searchStore.FindNamesByResourceAsync(
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
        string? type = await ReadTypeAsync(head.ResourceId, cassetteIds, cancellationToken);
        return new ProjectResourceSummary(
            head.ResourceId,
            displayName,
            type,
            type is null ? null : ontology?.LabelOf(type, preferredLanguage) ?? type,
            head.SourceCassetteId);
    }

    private async ValueTask<ResourceHead?> GetVisibleHeadAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        ResourceHead? head = await rdfStore.GetResourceHeadAsync(resourceId, cancellationToken);
        return head is null || head.IsDeleted || !cassetteIds.Contains(head.SourceCassetteId)
            ? null
            : head;
    }

    private async Task<string?> ReadTypeAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        List<string> types = new();
        await foreach (TripleRow triple in rdfStore.FindAsync(
                           new TriplePattern(
                               Subject: resourceId,
                               Predicate: RdfType,
                               ObjectKind: TripleObjectKind.Iri),
                           cassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            types.Add(triple.ObjectValue);
        }

        return types.Order(StringComparer.Ordinal).FirstOrDefault();
    }
}
