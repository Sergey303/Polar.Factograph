using System.Runtime.CompilerServices;
using Polar.DB.Typed;

namespace Polar.Factograph.Storage;

/// <summary>
/// Writes one complete project index generation through the existing Polar.DB.Typed DbSet implementation.
/// All external indexes are built before the filesystem CURRENT pointer is switched.
/// </summary>
public sealed class PolarDbTypedIndexGenerationWriter : IProjectIndexGenerationWriter
{
    private const string IndexBuildSentinel = "__polar_factograph_build_external_index__";

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

        BuildExternalIndexes();
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

    private void BuildExternalIndexes()
    {
        DbSet<PolarDbResourceHeadRow> resourceHeads = RequireResourceHeads();
        _ = resourceHeads.Find(row => row.SourceCassetteId, IndexBuildSentinel);

        DbSet<PolarDbTripleRow> triples = RequireTriples();
        _ = triples.Find(row => row.Subject, IndexBuildSentinel);
        _ = triples.Find(row => row.Predicate, IndexBuildSentinel);
        _ = triples.Find(row => row.ObjectValue, IndexBuildSentinel);
        _ = triples.Find(row => row.SourceCassetteId, IndexBuildSentinel);
        _ = triples.Find(row => row.SubjectPredicateKey, IndexBuildSentinel);
        _ = triples.Find(row => row.PredicateObjectKey, IndexBuildSentinel);

        DbSet<PolarDbNameSearchRow> names = RequireNameSearch();
        _ = names.Find(row => row.SearchKey, IndexBuildSentinel);
        _ = names.Find(row => row.ResourceId, IndexBuildSentinel);
        _ = names.Find(row => row.SourceCassetteId, IndexBuildSentinel);

        DbSet<PolarDbWordSearchRow> words = RequireWordSearch();
        _ = words.Find(row => row.Word, IndexBuildSentinel);
        _ = words.Find(row => row.ResourceId, IndexBuildSentinel);
        _ = words.Find(row => row.SourceCassetteId, IndexBuildSentinel);
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

/// <summary>
/// Read-only adapter over one completed project index generation.
/// A store instance remains bound to the generation that was current when it was opened.
/// </summary>
public sealed class PolarDbTypedProjectStore : IProjectRdfStore, IProjectSearchStore, IDisposable
{
    private readonly DbSet<PolarDbResourceHeadRow> _resourceHeads;
    private readonly DbSet<PolarDbTripleRow> _triples;
    private readonly DbSet<PolarDbNameSearchRow> _nameSearch;
    private readonly DbSet<PolarDbWordSearchRow> _wordSearch;
    private readonly Func<CancellationToken, Task>? _rebuild;
    private bool _disposed;

    private PolarDbTypedProjectStore(
        string generationPath,
        DbSet<PolarDbResourceHeadRow> resourceHeads,
        DbSet<PolarDbTripleRow> triples,
        DbSet<PolarDbNameSearchRow> nameSearch,
        DbSet<PolarDbWordSearchRow> wordSearch,
        Func<CancellationToken, Task>? rebuild)
    {
        GenerationPath = generationPath;
        _resourceHeads = resourceHeads;
        _triples = triples;
        _nameSearch = nameSearch;
        _wordSearch = wordSearch;
        _rebuild = rebuild;
    }

    public string GenerationPath { get; }

    public static PolarDbTypedProjectStore OpenCurrent(
        string indexRoot,
        Func<CancellationToken, Task>? rebuild = null)
    {
        string generationPath = FileSystemIndexGeneration.GetCurrentGenerationPath(indexRoot)
            ?? throw new FileNotFoundException(
                $"The project index has no CURRENT generation: {Path.GetFullPath(indexRoot)}");

        if (!Directory.Exists(generationPath))
        {
            throw new DirectoryNotFoundException(
                $"The project index CURRENT generation does not exist: {generationPath}");
        }

        DbSet<PolarDbResourceHeadRow>? resourceHeads = null;
        DbSet<PolarDbTripleRow>? triples = null;
        DbSet<PolarDbNameSearchRow>? nameSearch = null;
        DbSet<PolarDbWordSearchRow>? wordSearch = null;

        try
        {
            resourceHeads = PolarDbTypedSetFactory.OpenResourceHeads(generationPath);
            triples = PolarDbTypedSetFactory.OpenTriples(generationPath);
            nameSearch = PolarDbTypedSetFactory.OpenNameSearch(generationPath);
            wordSearch = PolarDbTypedSetFactory.OpenWordSearch(generationPath);

            return new PolarDbTypedProjectStore(
                generationPath,
                resourceHeads,
                triples,
                nameSearch,
                wordSearch,
                rebuild);
        }
        catch
        {
            wordSearch?.Dispose();
            nameSearch?.Dispose();
            triples?.Dispose();
            resourceHeads?.Dispose();
            throw;
        }
    }

    public ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        cancellationToken.ThrowIfCancellationRequested();

        ResourceHead? logical = _resourceHeads.TryGetByKey(resourceId, out PolarDbResourceHeadRow? row)
            ? PolarDbRowMapper.ToLogical(row)
            : null;
        return ValueTask.FromResult(logical);
    }

    public async IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);

        IReadOnlyList<PolarDbTripleRow> candidates = FindTripleCandidates(pattern);
        await Task.Yield();

        foreach (PolarDbTripleRow row in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!allowedCassetteIds.Contains(row.SourceCassetteId) || !Matches(row, pattern))
            {
                continue;
            }

            yield return PolarDbRowMapper.ToLogical(row);
        }
    }

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
        string normalizedSearchKey,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedSearchKey);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NameSearchHit> result = _nameSearch
            .Find(row => row.SearchKey, normalizedSearchKey)
            .Where(row => allowedCassetteIds.Contains(row.SourceCassetteId))
            .Select(ToNameSearchHit)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NameSearchHit> result = _nameSearch
            .Find(row => row.ResourceId, resourceId)
            .Where(row => allowedCassetteIds.Contains(row.SourceCassetteId))
            .Select(ToNameSearchHit)
            .Distinct()
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
        string normalizedWord,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedWord);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<WordSearchHit> result = _wordSearch
            .Find(row => row.Word, normalizedWord)
            .Where(row => allowedCassetteIds.Contains(row.SourceCassetteId))
            .Select(ToWordSearchHit)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _rebuild?.Invoke(cancellationToken)
            ?? throw new InvalidOperationException(
                "This project store was opened without an index rebuild callback.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _wordSearch.Dispose();
        _nameSearch.Dispose();
        _triples.Dispose();
        _resourceHeads.Dispose();
    }

    private IReadOnlyList<PolarDbTripleRow> FindTripleCandidates(TriplePattern pattern)
    {
        if (pattern.Subject is not null && pattern.Predicate is not null)
        {
            return _triples.Find(
                row => row.SubjectPredicateKey,
                PolarDbCompositeKey.Create(pattern.Subject, pattern.Predicate));
        }

        if (pattern.Predicate is not null &&
            pattern.ObjectKind is not null &&
            pattern.ObjectValue is not null)
        {
            return _triples.Find(
                row => row.PredicateObjectKey,
                PolarDbCompositeKey.Create(
                    pattern.Predicate,
                    ((int)pattern.ObjectKind.Value).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    pattern.ObjectValue));
        }

        if (pattern.Subject is not null)
        {
            return _triples.Find(row => row.Subject, pattern.Subject);
        }

        if (pattern.Predicate is not null)
        {
            return _triples.Find(row => row.Predicate, pattern.Predicate);
        }

        if (pattern.ObjectValue is not null)
        {
            return _triples.Find(row => row.ObjectValue, pattern.ObjectValue);
        }

        return _triples.All();
    }

    private static bool Matches(PolarDbTripleRow row, TriplePattern pattern) =>
        (pattern.Subject is null || string.Equals(row.Subject, pattern.Subject, StringComparison.Ordinal)) &&
        (pattern.Predicate is null || string.Equals(row.Predicate, pattern.Predicate, StringComparison.Ordinal)) &&
        (pattern.ObjectKind is null || row.ObjectKind == (int)pattern.ObjectKind.Value) &&
        (pattern.ObjectValue is null || string.Equals(row.ObjectValue, pattern.ObjectValue, StringComparison.Ordinal));

    private static NameSearchHit ToNameSearchHit(PolarDbNameSearchRow row) => new(
        row.ResourceId,
        row.Predicate,
        row.Value,
        EmptyToNull(row.Language),
        row.SourceCassetteId);

    private static WordSearchHit ToWordSearchHit(PolarDbWordSearchRow row) => new(
        row.ResourceId,
        row.Word,
        row.Predicate,
        row.Value,
        EmptyToNull(row.Language),
        row.SourceCassetteId);

    private static string? EmptyToNull(string value) =>
        string.IsNullOrEmpty(value) ? null : value;

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PolarDbTypedProjectStore));
        }
    }
}

internal static class PolarDbTypedSetFactory
{
    public static DbSet<PolarDbResourceHeadRow> OpenResourceHeads(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbPhysicalSchema.ResourceHeads.StorageName)
            .UseKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));

    public static DbSet<PolarDbTripleRow> OpenTriples(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbPhysicalSchema.Triples.StorageName)
            .UseKey(row => row.TripleId)
            .UseExternalKey(row => row.Subject)
            .UseExternalKey(row => row.Predicate)
            .UseExternalKey(row => row.ObjectValue)
            .UseExternalKey(row => row.SourceCassetteId)
            .UseExternalKey(row => row.SubjectPredicateKey)
            .UseExternalKey(row => row.PredicateObjectKey));

    public static DbSet<PolarDbNameSearchRow> OpenNameSearch(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbSearchPhysicalSchema.NameSearch.StorageName)
            .UseKey(row => row.SearchRowId)
            .UseExternalKey(row => row.SearchKey)
            .UseExternalKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));

    public static DbSet<PolarDbWordSearchRow> OpenWordSearch(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbSearchPhysicalSchema.WordSearch.StorageName)
            .UseKey(row => row.SearchRowId)
            .UseExternalKey(row => row.Word)
            .UseExternalKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));
}
