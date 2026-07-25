namespace Polar.Factograph.Fog;

internal sealed class FogMaterializationPlanBuilder
{
    public async Task<FogMaterializationPlan> BuildAsync(
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(openRecords);
        FogMaterializationScan scan = new();

        await foreach (FogSourceRecord record in openRecords(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            scan.Add(record);
        }

        IReadOnlyDictionary<string, string?> substitutions =
            FogSubstitutionResolver.Resolve(scan.Substitutions);
        IReadOnlyDictionary<string, FogRecordKey> winners =
            await FogDuplicateDefinitionSelector.SelectAsync(
                scan.DuplicateIds,
                openRecords,
                cancellationToken);
        bool containsCurrentRoot =
            scan.ContainsResource(LegacyFogVocabulary.CassetteRootCollectionId) &&
            !substitutions.ContainsKey(LegacyFogVocabulary.CassetteRootCollectionId);

        return new FogMaterializationPlan(
            substitutions,
            scan.DuplicateIds,
            winners,
            containsCurrentRoot,
            scan.SourceRecords,
            scan.ResourceDefinitions,
            scan.DeleteOperations,
            scan.SubstituteOperations);
    }
}
