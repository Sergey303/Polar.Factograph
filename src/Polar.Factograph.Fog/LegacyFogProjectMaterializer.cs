using System.Runtime.CompilerServices;

namespace Polar.Factograph.Fog;

public sealed class LegacyFogProjectMaterializer
{
    public async Task<FogMaterializationPlan> BuildPlanAsync(
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openRecords);

        Dictionary<string, string?> substitutions = new(StringComparer.Ordinal);
        HashSet<string> seenResourceIds = new(StringComparer.Ordinal);
        HashSet<string> duplicateIds = new(StringComparer.Ordinal);

        long sourceRecords = 0;
        long resourceDefinitions = 0;
        long deleteOperations = 0;
        long substituteOperations = 0;

        await foreach (FogSourceRecord record in openRecords(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            sourceRecords++;

            switch (record.Kind)
            {
                case FogRecordKind.Delete:
                    deleteOperations++;
                    substitutions[record.ResourceId] = null;
                    break;

                case FogRecordKind.Substitute:
                    substituteOperations++;
                    if (substitutions.TryGetValue(record.ResourceId, out string? existing) && existing is null)
                    {
                        break;
                    }

                    substitutions[record.ResourceId] = record.SubstituteTargetId
                        ?? throw new InvalidDataException(
                            $"Substitute target is absent for '{record.ResourceId}'.");
                    break;

                case FogRecordKind.Resource:
                    resourceDefinitions++;
                    if (!seenResourceIds.Add(record.ResourceId))
                    {
                        duplicateIds.Add(record.ResourceId);
                    }
                    break;

                default:
                    throw new InvalidDataException($"Unknown Fog record kind: {record.Kind}.");
            }
        }

        Dictionary<string, string?> resolvedSubstitutions = ResolveSubstitutions(substitutions);
        Dictionary<string, DefinitionWinner> winners = new(StringComparer.Ordinal);

        if (duplicateIds.Count > 0)
        {
            await foreach (FogSourceRecord record in openRecords(cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                if (record.Kind != FogRecordKind.Resource || !duplicateIds.Contains(record.ResourceId))
                {
                    continue;
                }

                if (!winners.TryGetValue(record.ResourceId, out DefinitionWinner winner) ||
                    record.ModifiedAt > winner.ModifiedAt)
                {
                    winners[record.ResourceId] = new DefinitionWinner(record.ModifiedAt, record.Key);
                }
            }
        }

        Dictionary<string, FogRecordKey> winningDefinitions = winners.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Key,
            StringComparer.Ordinal);

        bool containsCurrentCassetteRoot = seenResourceIds.Contains(LegacyFogVocabulary.CassetteRootCollectionId) &&
                                           !resolvedSubstitutions.ContainsKey(LegacyFogVocabulary.CassetteRootCollectionId);

        return new FogMaterializationPlan(
            resolvedSubstitutions,
            duplicateIds,
            winningDefinitions,
            containsCurrentCassetteRoot,
            sourceRecords,
            resourceDefinitions,
            deleteOperations,
            substituteOperations);
    }

    public async IAsyncEnumerable<FogCurrentRecord> ReadCurrentAsync(
        FogMaterializationPlan plan,
        FogRecordStreamFactory openRecords,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(openRecords);

        if (!plan.ContainsCurrentCassetteRoot)
        {
            yield return CreateSyntheticCassetteRoot();
        }

        await foreach (FogSourceRecord record in openRecords(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (!plan.Includes(record))
            {
                continue;
            }

            FogProperty[] properties = record.Properties
                .Select(property => property.Kind == FogPropertyKind.Resource
                    ? property with { Value = plan.ResolveReferencedId(property.Value) }
                    : property)
                .ToArray();

            yield return new FogCurrentRecord(
                record.ResourceId,
                record.Type ?? throw new InvalidDataException(
                    $"Resource '{record.ResourceId}' has no type."),
                record.ModifiedAt,
                properties,
                record.SourceCassetteId,
                record.SourceCassetteName,
                record.Key.SourceFogPath,
                record.Key.SourceOrdinal,
                IsSynthetic: false);
        }
    }

    public async Task<FogMaterializationStatistics> SummarizeAsync(
        int sourceFiles,
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default)
    {
        FogMaterializationPlan plan = await BuildPlanAsync(openRecords, cancellationToken);
        long currentSourceResources = 0;
        int syntheticResources = 0;
        long currentProperties = 0;

        await foreach (FogCurrentRecord record in ReadCurrentAsync(plan, openRecords, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (record.IsSynthetic)
            {
                syntheticResources++;
            }
            else
            {
                currentSourceResources++;
            }

            currentProperties += record.Properties.Count;
        }

        return new FogMaterializationStatistics(
            sourceFiles,
            plan.SourceRecords,
            plan.ResourceDefinitions,
            plan.DeleteOperations,
            plan.SubstituteOperations,
            plan.DuplicateResourceIds,
            plan.RedirectedIds,
            plan.DeletedIds,
            currentSourceResources,
            syntheticResources,
            currentProperties);
    }

    private static Dictionary<string, string?> ResolveSubstitutions(
        IReadOnlyDictionary<string, string?> substitutions)
    {
        Dictionary<string, string?> result = new(StringComparer.Ordinal);

        foreach ((string sourceId, string? directTarget) in substitutions)
        {
            result[sourceId] = directTarget is null
                ? null
                : ResolveTarget(sourceId, directTarget, substitutions);
        }

        return result;
    }

    private static string ResolveTarget(
        string sourceId,
        string directTarget,
        IReadOnlyDictionary<string, string?> substitutions)
    {
        HashSet<string> visited = new(StringComparer.Ordinal) { sourceId };
        string current = directTarget;

        while (substitutions.TryGetValue(current, out string? next))
        {
            if (!visited.Add(current))
            {
                string chain = string.Join(" -> ", visited.Append(current));
                throw new InvalidDataException($"Cyclic Fog substitute chain: {chain}.");
            }

            // Legacy behavior stops at a deleted target. References are redirected to that
            // target id, while the deleted target itself remains absent from current records.
            if (next is null)
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    private static FogCurrentRecord CreateSyntheticCassetteRoot() => new(
        LegacyFogVocabulary.CassetteRootCollectionId,
        LegacyFogVocabulary.Namespace + "collection",
        DateTime.MinValue,
        new[]
        {
            new FogProperty(
                LegacyFogVocabulary.Namespace + "name",
                FogPropertyKind.Literal,
                "кассеты")
        },
        SourceCassetteId: null,
        SourceCassetteName: null,
        SourceFogPath: null,
        SourceOrdinal: null,
        IsSynthetic: true);

    private readonly record struct DefinitionWinner(DateTime ModifiedAt, FogRecordKey Key);
}
