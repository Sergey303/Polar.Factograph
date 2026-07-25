using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

internal sealed class ProjectWordSearchExecutor(
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
        IReadOnlyList<string> words = LegacySearchIndexProjector.NormalizeSearchWords(query);
        if (words.Count == 0)
        {
            return Array.Empty<ProjectResourceSearchResult>();
        }

        HashSet<string> cassetteIds = ProjectSearchRules.EffectiveCassetteIds(allowedCassetteIds);
        Dictionary<string, ProjectWordCandidateBuilder> candidates = new(StringComparer.Ordinal);

        foreach (string word in words)
        {
            IReadOnlyList<WordSearchHit> hits = await searchStore.FindWordAsync(
                word,
                cassetteIds,
                cancellationToken);
            foreach (WordSearchHit hit in hits)
            {
                if (!candidates.TryGetValue(hit.ResourceId, out ProjectWordCandidateBuilder? candidate))
                {
                    candidate = new ProjectWordCandidateBuilder(hit.ResourceId);
                    candidates.Add(hit.ResourceId, candidate);
                }

                candidate.Add(word, hit);
            }
        }

        List<ProjectRankedCandidate> ranked = new();
        foreach (ProjectWordCandidateBuilder candidate in candidates.Values
                     .OrderByDescending(candidate => candidate.Score)
                     .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal)
                     .Take(limit * 10))
        {
            IReadOnlyList<NameSearchHit> names = await searchStore.FindNamesByResourceAsync(
                candidate.ResourceId,
                cassetteIds,
                cancellationToken);
            ranked.Add(new ProjectRankedCandidate(
                candidate.ResourceId,
                ProjectSearchRules.SelectDisplayName(
                    names,
                    preferredLanguage,
                    candidate.ResourceId),
                candidate.Score,
                candidate.Matches));
        }

        return await enricher.EnrichAsync(
            ranked
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(candidate => candidate.ResourceId, StringComparer.Ordinal),
            cassetteIds,
            limit,
            preferredLanguage,
            cancellationToken);
    }
}
