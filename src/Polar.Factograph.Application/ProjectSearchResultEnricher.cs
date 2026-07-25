namespace Polar.Factograph.Application;

internal sealed class ProjectSearchResultEnricher(ProjectResourceSummaryReader summaryReader)
{
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
            ProjectResourceSummary? summary = await summaryReader.ReadAsync(
                candidate.ResourceId,
                candidate.DisplayName,
                cassetteIds,
                preferredLanguage,
                cancellationToken);
            if (summary is null)
            {
                continue;
            }

            results.Add(new ProjectResourceSearchResult(
                summary.ResourceId,
                summary.DisplayName,
                summary.Type,
                summary.TypeLabel,
                candidate.Score,
                summary.SourceCassetteId,
                candidate.Matches));

            if (results.Count == limit) break;
        }

        return results;
    }
}
