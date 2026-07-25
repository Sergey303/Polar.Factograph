using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectSearchResultEnricher(
    IProjectRdfStore rdfStore,
    OntologyCatalog? ontology)
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

    public async Task<IReadOnlyList<ProjectResourceSearchResult>> EnrichAsync(
        IEnumerable<ProjectRankedCandidate> candidates,
        IReadOnlySet<string> cassetteIds,
        int limit,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        List<ProjectResourceSearchResult> results = new(limit);

        foreach (ProjectRankedCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ResourceHead? head = await rdfStore.GetResourceHeadAsync(
                candidate.ResourceId,
                cancellationToken);
            if (head is null || head.IsDeleted || !cassetteIds.Contains(head.SourceCassetteId))
            {
                continue;
            }

            string? type = await ReadTypeAsync(candidate.ResourceId, cassetteIds, cancellationToken);
            results.Add(new ProjectResourceSearchResult(
                candidate.ResourceId,
                candidate.DisplayName,
                type,
                type is null ? null : ontology?.LabelOf(type, preferredLanguage) ?? type,
                candidate.Score,
                head.SourceCassetteId,
                candidate.Matches));

            if (results.Count == limit) break;
        }

        return results;
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
