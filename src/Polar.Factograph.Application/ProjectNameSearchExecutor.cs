using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectNameSearchExecutor(
    IProjectSearchStore searchStore,
    ProjectSearchResultEnricher enricher)
{
    public async Task<IReadOnlyList<ProjectResourceSearchResult>> SearchAsync(
        string query,
        IReadOnlySet<string> allowedCassetteIds,
        int limit,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        ProjectSearchRules.Validate(query, allowedCassetteIds, limit, preferredLanguage);
        string searchKey = LegacySearchIndexProjector.NormalizeNameQuery(query);
        if (searchKey.Length == 0)
        {
            return Array.Empty<ProjectResourceSearchResult>();
        }

        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(allowedCassetteIds);
        IReadOnlyList<NameSearchHit> hits = await searchStore.FindNamesByKeyAsync(
            searchKey,
            cassetteIds,
            cancellationToken);

        ProjectRankedCandidate[] candidates = hits
            .GroupBy(hit => hit.ResourceId, StringComparer.Ordinal)
            .Select(group => BuildCandidate(group.Key, group.ToArray(), searchKey, preferredLanguage))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal)
            .ToArray();

        return await enricher.EnrichAsync(
            candidates,
            cassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
    }

    private static ProjectRankedCandidate BuildCandidate(
        string resourceId,
        IReadOnlyList<NameSearchHit> hits,
        string searchKey,
        string preferredLanguage)
    {
        NameSearchHit[] distinctHits = hits.Distinct().ToArray();
        return new ProjectRankedCandidate(
            resourceId,
            ProjectSearchRules.SelectDisplayName(distinctHits, preferredLanguage, resourceId),
            distinctHits.Max(hit => ProjectSearchRules.NameScore(hit.Value, searchKey)),
            distinctHits
                .Select(ProjectSearchRules.ToEvidence)
                .OrderBy(evidence => evidence.Predicate, StringComparer.Ordinal)
                .ThenBy(evidence => evidence.Value, StringComparer.Ordinal)
                .ToArray());
    }
}
