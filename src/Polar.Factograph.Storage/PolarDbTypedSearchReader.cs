namespace Polar.Factograph.Storage;

internal sealed class PolarDbTypedSearchReader(PolarDbTypedStoreSets sets)
{
    public Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
        string normalizedSearchKey,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        sets.ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedSearchKey);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NameSearchHit> result = sets.NameSearch
            .All()
            .Where(row =>
                string.Equals(row.SearchKey, normalizedSearchKey, StringComparison.Ordinal) &&
                allowedCassetteIds.Contains(row.SourceCassetteId))
            .Select(ToNameSearchHit)
            .ToArray();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default)
    {
        sets.ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<NameSearchHit> result = sets.NameSearch
            .All()
            .Where(row =>
                string.Equals(row.ResourceId, resourceId, StringComparison.Ordinal) &&
                allowedCassetteIds.Contains(row.SourceCassetteId))
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
        sets.ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedWord);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<WordSearchHit> result = sets.WordSearch
            .All()
            .Where(row =>
                string.Equals(row.Word, normalizedWord, StringComparison.Ordinal) &&
                allowedCassetteIds.Contains(row.SourceCassetteId))
            .Select(ToWordSearchHit)
            .ToArray();
        return Task.FromResult(result);
    }

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
}
