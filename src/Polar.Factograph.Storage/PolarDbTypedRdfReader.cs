using System.Runtime.CompilerServices;

namespace Polar.Factograph.Storage;

internal sealed class PolarDbTypedRdfReader(PolarDbTypedStoreSets sets)
{
    public ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        sets.ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        cancellationToken.ThrowIfCancellationRequested();

        ResourceHead? logical = sets.ResourceHeads.TryGetByKey(
            resourceId,
            out PolarDbResourceHeadRow? row)
            ? PolarDbRowMapper.ToLogical(row)
            : null;
        return ValueTask.FromResult(logical);
    }

    public async IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        sets.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(allowedCassetteIds);

        // Compatibility path for the current Polar.DB.Typed external-key format.
        // The active generation can be enumerated and verified successfully, while
        // an external-key lookup may return an invalid record offset for a table
        // whose primary key is Guid. Until that index format is fixed and versioned,
        // prefer the authoritative table rows over a secondary acceleration structure.
        IReadOnlyList<PolarDbTripleRow> candidates = sets.Triples.All();
        await Task.Yield();

        foreach (PolarDbTripleRow row in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (allowedCassetteIds.Contains(row.SourceCassetteId) && Matches(row, pattern))
            {
                yield return PolarDbRowMapper.ToLogical(row);
            }
        }
    }

    private static bool Matches(PolarDbTripleRow row, TriplePattern pattern) =>
        (pattern.Subject is null || string.Equals(row.Subject, pattern.Subject, StringComparison.Ordinal)) &&
        (pattern.Predicate is null || string.Equals(row.Predicate, pattern.Predicate, StringComparison.Ordinal)) &&
        (pattern.ObjectKind is null || row.ObjectKind == (int)pattern.ObjectKind.Value) &&
        (pattern.ObjectValue is null || string.Equals(row.ObjectValue, pattern.ObjectValue, StringComparison.Ordinal));
}
