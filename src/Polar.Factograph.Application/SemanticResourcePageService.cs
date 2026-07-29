namespace Polar.Factograph.Application;

public sealed class SemanticResourcePageService(
    AuthorizedProjectReadService reads,
    OntologyResourcePortraitPresenter presenter,
    OntologyCatalog ontology)
{
    public async ValueTask<PresentedSemanticResourcePage?> GetAsync(
        string resourceId,
        ProjectAccessSnapshot access,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        SemanticResourceGraph graph = new(
            reads,
            presenter,
            ontology,
            access,
            preferredLanguage);
        ProjectResourcePortrait? requested = await graph.GetAsync(resourceId, cancellationToken);
        if (requested is null)
        {
            return null;
        }

        ProjectResourcePortrait root = await graph.ResolveCanonicalAsync(
            requested,
            cancellationToken);
        if (graph.IsTechnical(root))
        {
            return null;
        }

        IReadOnlyList<SemanticPhotoCard> photos = await new SemanticPhotoCollector(graph)
            .CollectAsync(root, cancellationToken);
        IReadOnlyList<SemanticResourceLink> participants = await CollectParticipantsAsync(
            graph,
            root,
            cancellationToken);
        IReadOnlyList<SemanticResourceLink> organizations = await CollectOrganizationsAsync(
            graph,
            root,
            cancellationToken);
        IReadOnlyList<SemanticResourceLink> collections = await CollectCollectionsAsync(
            graph,
            root,
            cancellationToken);
        IReadOnlyList<SemanticResourceLink> related = await CollectRelatedAsync(
            graph,
            root,
            cancellationToken);

        return new PresentedSemanticResourcePage(
            resourceId,
            graph.Present(root),
            photos,
            participants,
            organizations,
            collections,
            related);
    }

    private static async Task<IReadOnlyList<SemanticResourceLink>> CollectParticipantsAsync(
        SemanticResourceGraph graph,
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SemanticResourceLink> result = new(StringComparer.Ordinal);

        if (graph.IsType(root, SemanticBridgeVocabulary.Organization))
        {
            foreach (string bridgeId in graph.InverseSources(
                         root,
                         SemanticBridgeVocabulary.InOrganization))
            {
                ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
                if (bridge?.Type != SemanticBridgeVocabulary.Participation)
                {
                    continue;
                }

                string relation = graph.LiteralValue(bridge, SemanticBridgeVocabulary.Role)
                    ?? "участник";
                foreach (string participantId in graph.DirectTargets(
                             bridge,
                             SemanticBridgeVocabulary.Participant))
                {
                    await AddLinkAsync(
                        result,
                        graph,
                        participantId,
                        relation,
                        cancellationToken,
                        bridge);
                }
            }
        }

        if (graph.IsType(root, SemanticBridgeVocabulary.Collection))
        {
            foreach (string bridgeId in graph.InverseSources(
                         root,
                         SemanticBridgeVocabulary.InCollection))
            {
                ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
                if (bridge?.Type != SemanticBridgeVocabulary.CollectionMember)
                {
                    continue;
                }

                foreach (string itemId in graph.DirectTargets(
                             bridge,
                             SemanticBridgeVocabulary.CollectionItem))
                {
                    ProjectResourcePortrait? item = await graph.GetAsync(itemId, cancellationToken);
                    if (item is null || graph.IsType(item, SemanticBridgeVocabulary.Document))
                    {
                        continue;
                    }

                    await AddLinkAsync(
                        result,
                        graph,
                        itemId,
                        "элемент коллекции",
                        cancellationToken,
                        bridge);
                }
            }
        }

        return Sort(result.Values);
    }

    private static async Task<IReadOnlyList<SemanticResourceLink>> CollectOrganizationsAsync(
        SemanticResourceGraph graph,
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SemanticResourceLink> result = new(StringComparer.Ordinal);
        foreach (string bridgeId in graph.InverseSources(
                     root,
                     SemanticBridgeVocabulary.Participant))
        {
            ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
            if (bridge?.Type != SemanticBridgeVocabulary.Participation)
            {
                continue;
            }

            string relation = graph.LiteralValue(bridge, SemanticBridgeVocabulary.Role)
                ?? "участие";
            foreach (string organizationId in graph.DirectTargets(
                         bridge,
                         SemanticBridgeVocabulary.InOrganization))
            {
                await AddLinkAsync(
                    result,
                    graph,
                    organizationId,
                    relation,
                    cancellationToken,
                    bridge);
            }
        }

        return Sort(result.Values);
    }

    private static async Task<IReadOnlyList<SemanticResourceLink>> CollectCollectionsAsync(
        SemanticResourceGraph graph,
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SemanticResourceLink> result = new(StringComparer.Ordinal);
        await AddContainingCollectionsAsync(result, graph, root, cancellationToken);

        foreach (string reflectionId in graph.InverseSources(
                     root,
                     SemanticBridgeVocabulary.Reflected))
        {
            ProjectResourcePortrait? reflection = await graph.GetAsync(
                reflectionId,
                cancellationToken);
            if (reflection?.Type != SemanticBridgeVocabulary.Reflection)
            {
                continue;
            }

            foreach (string documentId in graph.DirectTargets(
                         reflection,
                         SemanticBridgeVocabulary.InDocument))
            {
                ProjectResourcePortrait? document = await graph.GetAsync(
                    documentId,
                    cancellationToken);
                if (document is not null)
                {
                    await AddContainingCollectionsAsync(
                        result,
                        graph,
                        document,
                        cancellationToken);
                }
            }
        }

        return Sort(result.Values);
    }

    private static async Task AddContainingCollectionsAsync(
        IDictionary<string, SemanticResourceLink> result,
        SemanticResourceGraph graph,
        ProjectResourcePortrait item,
        CancellationToken cancellationToken)
    {
        foreach (string bridgeId in graph.InverseSources(
                     item,
                     SemanticBridgeVocabulary.CollectionItem))
        {
            ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
            if (bridge?.Type != SemanticBridgeVocabulary.CollectionMember)
            {
                continue;
            }

            foreach (string collectionId in graph.DirectTargets(
                         bridge,
                         SemanticBridgeVocabulary.InCollection))
            {
                await AddLinkAsync(
                    result,
                    graph,
                    collectionId,
                    "в коллекции",
                    cancellationToken,
                    bridge);
            }
        }
    }

    private static async Task<IReadOnlyList<SemanticResourceLink>> CollectRelatedAsync(
        SemanticResourceGraph graph,
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        Dictionary<string, SemanticResourceLink> result = new(StringComparer.Ordinal);
        foreach (ResourceDirectLink link in root.DirectLinks)
        {
            ProjectResourcePortrait? target = await graph.GetAsync(
                link.TargetResourceId,
                cancellationToken);
            if (target is not null && graph.IsComplexRelation(target))
            {
                if (!graph.IsTechnical(target))
                {
                    await AddComplexRelationTargetsAsync(
                        result,
                        graph,
                        root,
                        target,
                        BuildComplexRelationLabel(
                            graph.PropertyLabel(link.Predicate),
                            graph.TypeLabel(target)),
                        cancellationToken);
                }
                continue;
            }

            await AddLinkAsync(
                result,
                graph,
                link.TargetResourceId,
                graph.PropertyLabel(link.Predicate),
                cancellationToken);
        }

        foreach (ResourceInverseLink link in root.InverseLinks)
        {
            ProjectResourcePortrait? source = await graph.GetAsync(
                link.SourceResourceId,
                cancellationToken);
            if (source is not null && graph.IsComplexRelation(source))
            {
                if (!graph.IsTechnical(source))
                {
                    await AddComplexRelationTargetsAsync(
                        result,
                        graph,
                        root,
                        source,
                        BuildComplexRelationLabel(
                            graph.InversePropertyLabel(link.Predicate),
                            graph.TypeLabel(source)),
                        cancellationToken);
                }
                continue;
            }

            await AddLinkAsync(
                result,
                graph,
                link.SourceResourceId,
                graph.InversePropertyLabel(link.Predicate),
                cancellationToken);
        }

        return Sort(result.Values);
    }

    private static async Task AddComplexRelationTargetsAsync(
        IDictionary<string, SemanticResourceLink> result,
        SemanticResourceGraph graph,
        ProjectResourcePortrait root,
        ProjectResourcePortrait relation,
        string relationLabel,
        CancellationToken cancellationToken)
    {
        foreach (ResourceDirectLink targetLink in relation.DirectLinks)
        {
            if (string.Equals(
                    targetLink.TargetResourceId,
                    root.ResourceId,
                    StringComparison.Ordinal))
            {
                continue;
            }

            ProjectResourcePortrait? target = await graph.GetAsync(
                targetLink.TargetResourceId,
                cancellationToken);
            if (target is null || !graph.IsEntity(target))
            {
                continue;
            }

            await AddLinkAsync(
                result,
                graph,
                target.ResourceId,
                relationLabel,
                cancellationToken,
                relation);
        }
    }

    private static string BuildComplexRelationLabel(string roleLabel, string? relationTypeLabel) =>
        string.IsNullOrWhiteSpace(relationTypeLabel)
            ? roleLabel
            : $"{roleLabel} · {relationTypeLabel}";

    private static async Task AddLinkAsync(
        IDictionary<string, SemanticResourceLink> result,
        SemanticResourceGraph graph,
        string resourceId,
        string relationLabel,
        CancellationToken cancellationToken,
        ProjectResourcePortrait? relation = null)
    {
        string key = relation is null
            ? $"{relationLabel}\n{resourceId}"
            : $"{relation.ResourceId}\n{resourceId}";
        if (result.ContainsKey(key))
        {
            return;
        }

        SemanticResourceLink? link = await graph.LinkAsync(
            resourceId,
            relationLabel,
            cancellationToken,
            relation);
        if (link is not null)
        {
            result.Add(key, link);
        }
    }

    private static SemanticResourceLink[] Sort(IEnumerable<SemanticResourceLink> links) =>
        links
            .OrderBy(link => link.SortDate is null ? 1 : 0)
            .ThenBy(link => link.SortDate, StringComparer.Ordinal)
            .ThenBy(link => link.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(link => link.RelationResourceId ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(link => link.ResourceId, StringComparer.Ordinal)
            .ToArray();
}
