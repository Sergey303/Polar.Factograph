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
        IReadOnlyList<ProjectNameQueryVariant> variants = ProjectNameQueryVariants.Create(query);
        if (variants.Count == 0)
        {
            return Array.Empty<ProjectResourceSearchResult>();
        }

        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(allowedCassetteIds);
        List<VariantNameHit> allHits = [];
        foreach (ProjectNameQueryVariant variant in variants)
        {
            IReadOnlyList<NameSearchHit> hits = await searchStore.FindNamesByKeyAsync(
                variant.Key,
                cassetteIds,
                cancellationToken);
            allHits.AddRange(hits.Select(hit => new VariantNameHit(hit, variant)));
        }

        ProjectRankedCandidate[] candidates = allHits
            .GroupBy(item => item.Hit.ResourceId, StringComparer.Ordinal)
            .Select(group => BuildCandidate(group.Key, group.ToArray(), preferredLanguage))
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
        IReadOnlyList<VariantNameHit> variantHits,
        string preferredLanguage)
    {
        NameSearchHit[] distinctHits = variantHits
            .Select(item => item.Hit)
            .Distinct()
            .ToArray();
        int score = variantHits.Max(item =>
            item.Variant.Rank + ProjectSearchRules.NameScore(
                item.Hit.Value,
                item.Variant.Key));
        return new ProjectRankedCandidate(
            resourceId,
            ProjectSearchRules.SelectDisplayName(distinctHits, preferredLanguage, resourceId),
            score,
            distinctHits
                .Select(ProjectSearchRules.ToEvidence)
                .OrderBy(evidence => evidence.Predicate, StringComparer.Ordinal)
                .ThenBy(evidence => evidence.Value, StringComparer.Ordinal)
                .ToArray());
    }

    private sealed record VariantNameHit(
        NameSearchHit Hit,
        ProjectNameQueryVariant Variant);
}
