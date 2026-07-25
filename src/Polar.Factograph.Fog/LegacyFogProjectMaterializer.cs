namespace Polar.Factograph.Fog;

public sealed class LegacyFogProjectMaterializer
{
    private readonly FogMaterializationPlanBuilder _plans;
    private readonly FogCurrentRecordReader _currentRecords;
    private readonly FogMaterializationStatisticsBuilder _statistics;

    public LegacyFogProjectMaterializer()
    {
        _plans = new FogMaterializationPlanBuilder();
        _currentRecords = new FogCurrentRecordReader();
        _statistics = new FogMaterializationStatisticsBuilder(_plans, _currentRecords);
    }

    public Task<FogMaterializationPlan> BuildPlanAsync(
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default) =>
        _plans.BuildAsync(openRecords, cancellationToken);

    public IAsyncEnumerable<FogCurrentRecord> ReadCurrentAsync(
        FogMaterializationPlan plan,
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default) =>
        _currentRecords.ReadAsync(plan, openRecords, cancellationToken);

    public Task<FogMaterializationStatistics> SummarizeAsync(
        int sourceFiles,
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken = default) =>
        _statistics.BuildAsync(sourceFiles, openRecords, cancellationToken);
}
