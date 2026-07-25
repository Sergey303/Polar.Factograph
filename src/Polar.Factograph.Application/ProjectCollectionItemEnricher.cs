namespace Polar.Factograph.Application;

internal sealed class ProjectCollectionItemEnricher(ProjectResourceSummaryReader summaryReader)
{
    public async Task<IReadOnlyList<ProjectCollectionItem>> EnrichAsync(
        IEnumerable<ProjectCollectionItemReference> references,
        IReadOnlySet<string> cassetteIds,
        int limit,
        string preferredLanguage,
        CancellationToken cancellationToken)
    {
        List<ProjectCollectionItem> items = new();

        foreach (ProjectCollectionItemReference reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectResourceSummary? summary = await summaryReader.ReadAsync(
                reference.ResourceId,
                cassetteIds,
                preferredLanguage,
                cancellationToken);
            if (summary is null)
            {
                continue;
            }

            items.Add(new ProjectCollectionItem(
                reference.MembershipResourceId,
                summary.ResourceId,
                summary.DisplayName,
                summary.Type,
                summary.TypeLabel,
                reference.MembershipCassetteId,
                summary.SourceCassetteId));
        }

        return items
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ResourceId, StringComparer.Ordinal)
            .ThenBy(item => item.MembershipResourceId, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();
    }
}
