using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed record ProjectResourceSummary(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string SourceCassetteId);

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
        ResourceHead? head = await rdfStore.GetResourceHeadAsync(resourceId, cancellationToken);
        if (head is null || head.IsDeleted || !cassetteIds.Contains(head.SourceCassetteId))
        {
            return null;
        }

        IReadOnlyList<NameSearchHit> names = await searchStore.FindNamesByResourceAsync(
            resourceId,
            cassetteIds,
            cancellationToken);
        string? type = await ReadTypeAsync(resourceId, cassetteIds, cancellationToken);

        return new ProjectResourceSummary(
            resourceId,
            ProjectSearchRules.SelectDisplayName(names, preferredLanguage, resourceId),
            type,
            type is null ? null : ontology?.LabelOf(type, preferredLanguage) ?? type,
            head.SourceCassetteId);
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
