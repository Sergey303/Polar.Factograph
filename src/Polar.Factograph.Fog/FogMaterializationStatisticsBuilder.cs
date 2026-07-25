namespace Polar.Factograph.Fog;

internal sealed class FogMaterializationStatisticsBuilder(
    FogMaterializationPlanBuilder plans,
    FogCurrentRecordReader currentRecords)
{
    public async Task<FogMaterializationStatistics> BuildAsync(
        int sourceFiles,
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default)
    {
        FogMaterializationPlan plan = await plans.BuildAsync(openRecords, cancellationToken);
        long currentSourceResources = 0;
        int syntheticResources = 0;
        long currentProperties = 0;

        await foreach (FogCurrentRecord record in currentRecords.ReadAsync(
                           plan,
                           openRecords,
                           cancellationToken)
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
}
