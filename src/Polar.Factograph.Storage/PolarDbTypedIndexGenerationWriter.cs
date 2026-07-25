using Polar.DB.Typed;

namespace Polar.Factograph.Storage;

/// <summary>
/// Writes one complete project index generation through the existing Polar.DB.Typed DbSet implementation.
/// All external indexes are built before the filesystem CURRENT pointer is switched.
/// </summary>
public sealed class PolarDbTypedIndexGenerationWriter : IProjectIndexGenerationWriter
{
    private readonly FileSystemIndexGeneration _generation;
    private DbSet<PolarDbResourceHeadRow>? _resourceHeads;
    private DbSet<PolarDbTripleRow>? _triples;
    private DbSet<PolarDbNameSearchRow>? _nameSearch;
    private DbSet<PolarDbWordSearchRow>? _wordSearch;
    private bool _committed;
    private bool _aborted;

    private PolarDbTypedIndexGenerationWriter(
        FileSystemIndexGeneration generation,
        DbSet<PolarDbResourceHeadRow> resourceHeads,
        DbSet<PolarDbTripleRow> triples,
        DbSet<PolarDbNameSearchRow> nameSearch,
        DbSet<PolarDbWordSearchRow> wordSearch)
    {
        _generation = generation;
        _resourceHeads = resourceHeads;
        _triples = triples;
        _nameSearch = nameSearch;
        _wordSearch = wordSearch;
    }

    public Guid GenerationId => _generation.GenerationId;

    public string StagingPath => _generation.StagingPath;

    public static PolarDbTypedIndexGenerationWriter Begin(
        string indexRoot,
        Guid? generationId = null)
    {
        FileSystemIndexGeneration generation = FileSystemIndexGeneration.Begin(indexRoot, generationId);
        DbSet<PolarDbResourceHeadRow>? resourceHeads = null;
        DbSet<PolarDbTripleRow>? triples = null;
        DbSet<PolarDbNameSearchRow>? nameSearch = null;
        DbSet<PolarDbWordSearchRow>? wordSearch = null;

        try
        {
            resourceHeads = PolarDbTypedSetFactory.OpenResourceHeads(generation.StagingPath);
            triples = PolarDbTypedSetFactory.OpenTriples(generation.StagingPath);
            nameSearch = PolarDbTypedSetFactory.OpenNameSearch(generation.StagingPath);
            wordSearch = PolarDbTypedSetFactory.OpenWordSearch(generation.StagingPath);

            return new PolarDbTypedIndexGenerationWriter(
                generation,
                resourceHeads,
                triples,
                nameSearch,
                wordSearch);
        }
        catch
        {
            wordSearch?.Dispose();
            nameSearch?.Dispose();
            triples?.Dispose();
            resourceHeads?.Dispose();
            generation.AbortAsync(CancellationToken.None).GetAwaiter().GetResult();
            generation.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }
    }

    public ValueTask WriteResourceAsync(
        PolarDbResourceHeadRow resource,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        cancellationToken.ThrowIfCancellationRequested();
        RequireResourceHeads().Append(resource);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteTriplesAsync(
        IReadOnlyList<PolarDbTripleRow> triples,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(triples);
        cancellationToken.ThrowIfCancellationRequested();
        if (triples.Count > 0)
        {
            RequireTriples().AddRange(triples);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask WriteNameSearchRowsAsync(
        IReadOnlyList<PolarDbNameSearchRow> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();
        if (rows.Count > 0)
        {
            RequireNameSearch().AddRange(rows);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask WriteWordSearchRowsAsync(
        IReadOnlyList<PolarDbWordSearchRow> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        cancellationToken.ThrowIfCancellationRequested();
        if (rows.Count > 0)
        {
            RequireWordSearch().AddRange(rows);
        }

        return ValueTask.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFinished();
        cancellationToken.ThrowIfCancellationRequested();

        PolarDbTypedExternalIndexBuilder.Build(
            RequireResourceHeads(),
            RequireTriples(),
            RequireNameSearch(),
            RequireWordSearch());
        DisposeSets();
        await _generation.CommitAsync(cancellationToken);
        _committed = true;
    }

    public async Task AbortAsync(CancellationToken cancellationToken = default)
    {
        if (_committed || _aborted)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        DisposeSets();
        await _generation.AbortAsync(cancellationToken);
        _aborted = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_committed && !_aborted)
        {
            await AbortAsync(CancellationToken.None);
        }

        await _generation.DisposeAsync();
    }

    private void DisposeSets()
    {
        _wordSearch?.Dispose();
        _wordSearch = null;
        _nameSearch?.Dispose();
        _nameSearch = null;
        _triples?.Dispose();
        _triples = null;
        _resourceHeads?.Dispose();
        _resourceHeads = null;
    }

    private DbSet<PolarDbResourceHeadRow> RequireResourceHeads() =>
        _resourceHeads ?? throw FinishedException();

    private DbSet<PolarDbTripleRow> RequireTriples() =>
        _triples ?? throw FinishedException();

    private DbSet<PolarDbNameSearchRow> RequireNameSearch() =>
        _nameSearch ?? throw FinishedException();

    private DbSet<PolarDbWordSearchRow> RequireWordSearch() =>
        _wordSearch ?? throw FinishedException();

    private void ThrowIfFinished()
    {
        if (_committed || _aborted)
        {
            throw FinishedException();
        }
    }

    private static InvalidOperationException FinishedException() =>
        new("The Polar.DB.Typed index generation writer is already finished.");
}
