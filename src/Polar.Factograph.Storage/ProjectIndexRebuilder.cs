using System.Runtime.CompilerServices;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Storage;

public sealed record ProjectIndexBuildStatistics(
    long Resources,
    long Triples);

public interface IProjectIndexGenerationWriter : IAsyncDisposable
{
    ValueTask WriteResourceAsync(
        PolarDbResourceHeadRow resource,
        CancellationToken cancellationToken = default);

    ValueTask WriteTriplesAsync(
        IReadOnlyList<PolarDbTripleRow> triples,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task AbortAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Projects the current Fog cloud into physical rows and commits the generation only after the complete stream succeeds.
/// </summary>
public sealed class ProjectIndexRebuilder
{
    private readonly CurrentRecordTripleProjector _projector;

    public ProjectIndexRebuilder(CurrentRecordTripleProjector? projector = null)
    {
        _projector = projector ?? new CurrentRecordTripleProjector();
    }

    public async Task<ProjectIndexBuildStatistics> RebuildAsync(
        IAsyncEnumerable<FogCurrentRecord> currentRecords,
        IProjectIndexGenerationWriter writer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentRecords);
        ArgumentNullException.ThrowIfNull(writer);

        long resources = 0;
        long triples = 0;

        try
        {
            await foreach (FogCurrentRecord current in currentRecords
                               .WithCancellation(cancellationToken))
            {
                ProjectedResource projected = _projector.Project(current);
                PolarDbResourceHeadRow physicalHead = PolarDbRowMapper.ToPhysical(projected.Head);
                PolarDbTripleRow[] physicalTriples = projected.Triples
                    .Select(PolarDbRowMapper.ToPhysical)
                    .ToArray();

                await writer.WriteResourceAsync(physicalHead, cancellationToken);
                await writer.WriteTriplesAsync(physicalTriples, cancellationToken);

                resources++;
                triples += physicalTriples.Length;
            }

            await writer.CommitAsync(cancellationToken);
            return new ProjectIndexBuildStatistics(resources, triples);
        }
        catch
        {
            await TryAbortAsync(writer);
            throw;
        }
    }

    private static async Task TryAbortAsync(IProjectIndexGenerationWriter writer)
    {
        try
        {
            await writer.AbortAsync(CancellationToken.None);
        }
        catch
        {
            // Preserve the original build exception. A concrete writer should log cleanup failures.
        }
    }
}
