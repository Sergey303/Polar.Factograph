using Polar.Factograph.Fog;

namespace Polar.Factograph.Storage;

public sealed record ProjectIndexBuildStatistics(
    long Resources,
    long Triples,
    long NameSearchRows,
    long WordSearchRows);

public interface IProjectIndexGenerationWriter : IAsyncDisposable
{
    ValueTask WriteResourceAsync(
        PolarDbResourceHeadRow resource,
        CancellationToken cancellationToken = default);

    ValueTask WriteTriplesAsync(
        IReadOnlyList<PolarDbTripleRow> triples,
        CancellationToken cancellationToken = default);

    ValueTask WriteNameSearchRowsAsync(
        IReadOnlyList<PolarDbNameSearchRow> rows,
        CancellationToken cancellationToken = default);

    ValueTask WriteWordSearchRowsAsync(
        IReadOnlyList<PolarDbWordSearchRow> rows,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task AbortAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Projects the current Fog cloud into physical rows and commits the generation only after the complete stream succeeds.
/// </summary>
public sealed class ProjectIndexRebuilder
{
    private readonly CurrentRecordTripleProjector _tripleProjector;
    private readonly LegacySearchIndexProjector _searchProjector;

    public ProjectIndexRebuilder(
        CurrentRecordTripleProjector? tripleProjector = null,
        LegacySearchIndexProjector? searchProjector = null)
    {
        _tripleProjector = tripleProjector ?? new CurrentRecordTripleProjector();
        _searchProjector = searchProjector ?? new LegacySearchIndexProjector();
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
        long nameSearchRows = 0;
        long wordSearchRows = 0;

        try
        {
            await foreach (FogCurrentRecord current in currentRecords
                               .WithCancellation(cancellationToken))
            {
                ProjectedResource projected = _tripleProjector.Project(current);
                PolarDbResourceHeadRow physicalHead = PolarDbRowMapper.ToPhysical(projected.Head);
                PolarDbTripleRow[] physicalTriples = projected.Triples
                    .Select(PolarDbRowMapper.ToPhysical)
                    .ToArray();
                SearchIndexProjection search = _searchProjector.Project(projected);

                await writer.WriteResourceAsync(physicalHead, cancellationToken);
                await writer.WriteTriplesAsync(physicalTriples, cancellationToken);
                await writer.WriteNameSearchRowsAsync(search.NameRows, cancellationToken);
                await writer.WriteWordSearchRowsAsync(search.WordRows, cancellationToken);

                resources++;
                triples += physicalTriples.Length;
                nameSearchRows += search.NameRows.Count;
                wordSearchRows += search.WordRows.Count;
            }

            await writer.CommitAsync(cancellationToken);
            return new ProjectIndexBuildStatistics(
                resources,
                triples,
                nameSearchRows,
                wordSearchRows);
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