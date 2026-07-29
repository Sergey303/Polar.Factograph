namespace Polar.Factograph.Application;

internal sealed class SemanticPhotoCollector(SemanticResourceGraph graph)
{
    public async Task<IReadOnlyList<SemanticPhotoCard>> CollectAsync(
        ProjectResourcePortrait root,
        CancellationToken cancellationToken)
    {
        Dictionary<string, ProjectResourcePortrait?> documents = new(StringComparer.Ordinal);
        await AddReflectedDocumentsAsync(root, documents, cancellationToken);

        if (graph.IsType(root, SemanticBridgeVocabulary.Collection))
        {
            await AddCollectionDocumentsAsync(root, documents, cancellationToken);
        }

        List<SemanticPhotoCard> result = new();
        foreach ((string documentId, ProjectResourcePortrait? relation) in documents)
        {
            SemanticPhotoCard? card = await BuildCardAsync(
                root,
                documentId,
                relation,
                cancellationToken);
            if (card is not null)
            {
                result.Add(card);
            }
        }

        return result
            .OrderBy(card => card.SortDate is null ? 1 : 0)
            .ThenBy(card => card.SortDate, StringComparer.Ordinal)
            .ThenBy(card => card.ContextLabel ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(card => card.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private async Task AddReflectedDocumentsAsync(
        ProjectResourcePortrait root,
        IDictionary<string, ProjectResourcePortrait?> documents,
        CancellationToken cancellationToken)
    {
        foreach (string bridgeId in graph.InverseSources(
                     root,
                     SemanticBridgeVocabulary.Reflected))
        {
            ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
            if (bridge?.Type != SemanticBridgeVocabulary.Reflection)
            {
                continue;
            }

            foreach (string documentId in graph.DirectTargets(
                         bridge,
                         SemanticBridgeVocabulary.InDocument))
            {
                AddDocument(documents, documentId, bridge);
            }
        }
    }

    private async Task AddCollectionDocumentsAsync(
        ProjectResourcePortrait collection,
        IDictionary<string, ProjectResourcePortrait?> documents,
        CancellationToken cancellationToken)
    {
        foreach (string bridgeId in graph.InverseSources(
                     collection,
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
                if (item is not null && graph.IsType(item, SemanticBridgeVocabulary.PhotoDocument))
                {
                    AddDocument(documents, itemId, bridge);
                }
            }
        }
    }

    private void AddDocument(
        IDictionary<string, ProjectResourcePortrait?> documents,
        string documentId,
        ProjectResourcePortrait relation)
    {
        if (!documents.TryGetValue(documentId, out ProjectResourcePortrait? existing))
        {
            documents.Add(documentId, relation);
            return;
        }

        SemanticDateValue? candidateDate = graph.DateValue(relation);
        SemanticDateValue? existingDate = existing is null ? null : graph.DateValue(existing);
        if (candidateDate is not null &&
            (existingDate is null ||
             string.CompareOrdinal(candidateDate.SortKey, existingDate.SortKey) < 0))
        {
            documents[documentId] = relation;
        }
    }

    private async Task<SemanticPhotoCard?> BuildCardAsync(
        ProjectResourcePortrait root,
        string documentId,
        ProjectResourcePortrait? relation,
        CancellationToken cancellationToken)
    {
        ProjectResourcePortrait? document = await graph.GetAsync(documentId, cancellationToken);
        if (document is null || !graph.IsType(document, SemanticBridgeVocabulary.PhotoDocument))
        {
            return null;
        }

        ProjectResourcePortrait? context = null;
        if (graph.IsType(root, SemanticBridgeVocabulary.Organization) ||
            graph.IsType(root, SemanticBridgeVocabulary.Collection))
        {
            context = root;
        }
        else
        {
            context = await FindOrganizationContextAsync(
                document,
                root.ResourceId,
                cancellationToken)
                ?? await FindCollectionContextAsync(document, cancellationToken);
        }

        SemanticDateValue? date = relation is null ? null : graph.DateValue(relation);
        date ??= graph.DateValue(document);

        return new SemanticPhotoCard(
            document.ResourceId,
            graph.DisplayName(document),
            graph.DocumentUri(document),
            context?.ResourceId,
            context is null ? null : graph.DisplayName(context),
            date?.Display,
            date?.SortKey);
    }

    private async Task<ProjectResourcePortrait?> FindOrganizationContextAsync(
        ProjectResourcePortrait document,
        string rootResourceId,
        CancellationToken cancellationToken)
    {
        foreach (string bridgeId in graph.InverseSources(
                     document,
                     SemanticBridgeVocabulary.InDocument))
        {
            ProjectResourcePortrait? bridge = await graph.GetAsync(bridgeId, cancellationToken);
            if (bridge?.Type != SemanticBridgeVocabulary.Reflection)
            {
                continue;
            }

            foreach (string targetId in graph.DirectTargets(
                         bridge,
                         SemanticBridgeVocabulary.Reflected))
            {
                if (string.Equals(targetId, rootResourceId, StringComparison.Ordinal))
                {
                    continue;
                }

                ProjectResourcePortrait? target = await graph.GetAsync(targetId, cancellationToken);
                if (target is not null && graph.IsType(target, SemanticBridgeVocabulary.Organization))
                {
                    return target;
                }
            }
        }

        return null;
    }

    private async Task<ProjectResourcePortrait?> FindCollectionContextAsync(
        ProjectResourcePortrait document,
        CancellationToken cancellationToken)
    {
        foreach (string bridgeId in graph.InverseSources(
                     document,
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
                ProjectResourcePortrait? collection = await graph.GetAsync(
                    collectionId,
                    cancellationToken);
                if (collection is not null &&
                    graph.IsType(collection, SemanticBridgeVocabulary.Collection))
                {
                    return collection;
                }
            }
        }

        return null;
    }
}
