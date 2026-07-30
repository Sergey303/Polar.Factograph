namespace Polar.Factograph.Application;

internal sealed class SemanticRelationEntryCollector(SemanticResourceGraph graph)
{
    public async Task<IReadOnlyList<SemanticRelationEntry>> CollectAsync(
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(root);

        Dictionary<string, SemanticRelationEntry> result = new(StringComparer.Ordinal);
        foreach (ResourceDirectLink link in root.DirectLinks)
        {
            ProjectResourcePortrait? target = await graph.GetAsync(
                link.TargetResourceId,
                cancellationToken);
            if (target is not null && graph.IsComplexRelation(target))
            {
                await AddComplexAsync(result, root, target, cancellationToken);
                continue;
            }

            await AddOrdinaryAsync(
                result,
                root,
                link.TargetResourceId,
                link.Predicate,
                graph.PropertyLabel(link.Predicate),
                isInverse: false,
                cancellationToken);
        }

        foreach (ResourceInverseLink link in root.InverseLinks)
        {
            ProjectResourcePortrait? source = await graph.GetAsync(
                link.SourceResourceId,
                cancellationToken);
            if (source is not null && graph.IsComplexRelation(source))
            {
                await AddComplexAsync(result, root, source, cancellationToken);
                continue;
            }

            await AddOrdinaryAsync(
                result,
                root,
                link.SourceResourceId,
                link.Predicate,
                graph.InversePropertyLabel(link.Predicate),
                isInverse: true,
                cancellationToken);
        }

        return result.Values
            .OrderBy(entry => entry.SortDate is null ? 1 : 0)
            .ThenBy(entry => entry.SortDate ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(entry => entry.GroupLabel, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();
    }

    private async Task AddComplexAsync(
        IDictionary<string, SemanticRelationEntry> result,
        ProjectResourcePortrait root,
        ProjectResourcePortrait relation,
        CancellationToken cancellationToken)
    {
        if (result.ContainsKey(relation.ResourceId)) return;

        Dictionary<string, MemberCandidate> candidates = new(StringComparer.Ordinal);
        foreach (ResourceDirectLink link in relation.DirectLinks)
        {
            await AddMemberAsync(
                candidates,
                root,
                link.TargetResourceId,
                graph.PropertyLabel(link.Predicate),
                $"direct:{link.Predicate}",
                cancellationToken);
        }

        foreach (ResourceInverseLink link in relation.InverseLinks)
        {
            await AddMemberAsync(
                candidates,
                root,
                link.SourceResourceId,
                graph.InversePropertyLabel(link.Predicate),
                $"inverse:{link.Predicate}",
                cancellationToken);
        }

        MemberCandidate[] orderedCandidates = candidates.Values
            .OrderBy(candidate => string.Equals(
                candidate.Member.ResourceId,
                root.ResourceId,
                StringComparison.Ordinal) ? 0 : 1)
            .ThenBy(candidate => candidate.Member.RoleLabel ?? string.Empty,
                StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(candidate => candidate.Member.DisplayName,
                StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(candidate => candidate.Member.ResourceId, StringComparer.Ordinal)
            .ToArray();

        SemanticDateValue? date = graph.DateValue(relation);
        if (date is null)
        {
            date = orderedCandidates
                .Where(candidate => candidate.Member.DocumentUri is not null)
                .Select(candidate => graph.DateValue(candidate.Portrait))
                .Where(value => value is not null)
                .Cast<SemanticDateValue>()
                .OrderBy(value => value.SortKey, StringComparer.Ordinal)
                .ThenBy(value => value.Display, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        string? relationDocument = graph.DocumentUri(relation);
        string? memberDocument = orderedCandidates
            .Where(candidate => !string.Equals(
                candidate.Member.ResourceId,
                root.ResourceId,
                StringComparison.Ordinal))
            .Select(candidate => candidate.Member.DocumentUri)
            .FirstOrDefault(value => value is not null)
            ?? orderedCandidates
                .Select(candidate => candidate.Member.DocumentUri)
                .FirstOrDefault(value => value is not null);
        string? typeLabel = graph.TypeLabel(relation);
        string displayName = graph.DisplayName(relation);
        string? role = graph.LiteralValue(relation, SemanticBridgeVocabulary.Role)?.Trim();
        string title = !string.IsNullOrWhiteSpace(role)
            ? role
            : !string.Equals(displayName, relation.ResourceId, StringComparison.Ordinal)
                ? displayName
                : typeLabel ?? "Связь";
        string groupKey = relation.Type ?? relation.ResourceId;
        string groupLabel = typeLabel ?? title;

        result.Add(relation.ResourceId, new SemanticRelationEntry(
            relation.ResourceId,
            title,
            relation.ResourceId,
            relation.Type,
            typeLabel,
            groupKey,
            groupLabel,
            date?.Display,
            date?.SortKey,
            relationDocument ?? memberDocument,
            orderedCandidates.Select(candidate => candidate.Member).ToArray()));
    }

    private async Task AddOrdinaryAsync(
        IDictionary<string, SemanticRelationEntry> result,
        ProjectResourcePortrait root,
        string resourceId,
        string predicate,
        string relationLabel,
        bool isInverse,
        CancellationToken cancellationToken)
    {
        ProjectResourcePortrait? portrait = await graph.GetAsync(resourceId, cancellationToken);
        if (portrait is null || graph.IsTechnical(portrait)) return;

        string direction = isInverse ? "inverse" : "direct";
        string key = $"{direction}:{predicate}\n{resourceId}";
        if (result.ContainsKey(key)) return;

        string? documentUri = graph.DocumentUri(portrait);
        SemanticDateValue? date = documentUri is null ? null : graph.DateValue(portrait);
        SemanticRelationMember member = new(
            portrait.ResourceId,
            graph.DisplayName(portrait),
            portrait.Type,
            graph.TypeLabel(portrait),
            RoleLabel: null,
            documentUri);
        result.Add(key, new SemanticRelationEntry(
            key,
            relationLabel,
            RelationResourceId: null,
            RelationType: null,
            RelationTypeLabel: null,
            GroupKey: $"{direction}:{predicate}",
            GroupLabel: relationLabel,
            date?.Display,
            date?.SortKey,
            documentUri,
            [member]));
    }

    private async Task AddMemberAsync(
        IDictionary<string, MemberCandidate> result,
        ProjectResourcePortrait root,
        string resourceId,
        string roleLabel,
        string roleKey,
        CancellationToken cancellationToken)
    {
        ProjectResourcePortrait? portrait = string.Equals(
            resourceId,
            root.ResourceId,
            StringComparison.Ordinal)
            ? root
            : await graph.GetAsync(resourceId, cancellationToken);
        if (portrait is null || graph.IsTechnical(portrait) || !graph.IsEntity(portrait)) return;

        string key = $"{roleKey}\n{resourceId}";
        result.TryAdd(key, new MemberCandidate(
            portrait,
            new SemanticRelationMember(
                portrait.ResourceId,
                graph.DisplayName(portrait),
                portrait.Type,
                graph.TypeLabel(portrait),
                roleLabel,
                graph.DocumentUri(portrait))));
    }

    private sealed record MemberCandidate(
        ProjectResourcePortrait Portrait,
        SemanticRelationMember Member);
}
