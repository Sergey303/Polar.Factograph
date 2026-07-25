namespace Polar.Factograph.Storage;

/// <summary>
/// Read-only adapter over one completed project index generation.
/// A store instance remains bound to the generation that was current when it was opened.
/// </summary>
public sealed class PolarDbTypedProjectStore : IProjectRdfStore, IProjectSearchStore, IDisposable
{
    private readonly PolarDbTypedStoreSets _sets;
    private readonly PolarDbTypedRdfReader _rdf;
    private readonly PolarDbTypedSearchReader _search;
    private readonly Func<CancellationToken, Task>? _rebuild;

    private PolarDbTypedProjectStore(
        PolarDbTypedStoreSets sets,
        Func<CancellationToken, Task>? rebuild)
    {
        _sets = sets;
        _rdf = new PolarDbTypedRdfReader(sets);
        _search = new PolarDbTypedSearchReader(sets);
        _rebuild = rebuild;
    }

    public string GenerationPath => _sets.GenerationPath;

    public static PolarDbTypedProjectStore OpenCurrent(
        string indexRoot,
        Func<CancellationToken, Task>? rebuild = null) =>
        new(PolarDbTypedStoreSets.OpenCurrent(indexRoot), rebuild);

    public ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default) =>
        _rdf.GetResourceHeadAsync(resourceId, cancellationToken);

    public IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        _rdf.FindAsync(pattern, allowedCassetteIds, cancellationToken);

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
        string normalizedSearchKey,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        _search.FindNamesByKeyAsync(normalizedSearchKey, allowedCassetteIds, cancellationToken);

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        _search.FindNamesByResourceAsync(resourceId, allowedCassetteIds, cancellationToken);

    public Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
        string normalizedWord,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default) =>
        _search.FindWordAsync(normalizedWord, allowedCassetteIds, cancellationToken);

    public Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        _sets.ThrowIfDisposed();
        return _rebuild?.Invoke(cancellationToken)
            ?? throw new InvalidOperationException(
                "This project store was opened without an index rebuild callback.");
    }

    public void Dispose() => _sets.Dispose();
}
