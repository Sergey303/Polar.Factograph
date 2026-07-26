using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectIndexRebuildResult(
    Guid GenerationId,
    int SourceFiles,
    ProjectIndexBuildStatistics Statistics);

public sealed class ProjectIndexCoordinator(
    IFogSourceScanner sourceScanner,
    FogProjectRecordSource recordSource,
    LegacyFogProjectMaterializer materializer,
    ProjectIndexRebuilder rebuilder,
    ProjectOperationGate operationGate,
    ProjectIndexDirtyMarker dirtyMarker)
{
    public async Task<ProjectIndexRebuildResult> RebuildAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        await using IAsyncDisposable lease = await operationGate.AcquireAsync(
            project.Index.Path,
            cancellationToken);
        return await RebuildUnderLeaseAsync(project, cancellationToken);
    }

    internal async Task<ProjectIndexRebuildResult> RebuildUnderLeaseAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
            project,
            cancellationToken);
        FogRecordStreamFactory openRecords = token => recordSource.ReadAsync(sources, token);
        FogMaterializationPlan plan = await materializer.BuildPlanAsync(
            openRecords,
            cancellationToken);

        await using PolarDbTypedIndexGenerationWriter writer =
            PolarDbTypedIndexGenerationWriter.Begin(project.Index.Path);
        ProjectIndexBuildStatistics statistics = await rebuilder.RebuildAsync(
            materializer.ReadCurrentAsync(plan, openRecords, cancellationToken),
            writer,
            cancellationToken);
        dirtyMarker.Clear(project.Index.Path);

        return new ProjectIndexRebuildResult(
            writer.GenerationId,
            sources.Count,
            statistics);
    }
}
