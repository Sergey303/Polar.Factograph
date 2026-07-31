using System.Globalization;
using System.Runtime.CompilerServices;

namespace Polar.Factograph.Storage;

internal sealed class PolarDbTypedRdfReader(
    PolarDbTypedStoreSets sets,
    PolarDbReadMode readMode)
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

        IReadOnlyList<PolarDbTripleRow> candidates = readMode switch
        {
            PolarDbReadMode.FullScan => sets.Triples.All(),
            PolarDbReadMode.ExternalIndexes => FindIndexedCandidates(pattern),
            _ => throw new InvalidOperationException(
                $"Unsupported Polar.DB read mode: {readMode}.")
        };
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

    private IReadOnlyList<PolarDbTripleRow> FindIndexedCandidates(TriplePattern pattern)
    {
        if (pattern.Subject is not null && pattern.Predicate is not null)
        {
            string key = PolarDbCompositeKey.Create(pattern.Subject, pattern.Predicate);
            return sets.Triples.Find(row => row.SubjectPredicateKey, key);
        }

        if (pattern.Predicate is not null &&
            pattern.ObjectKind is not null &&
            pattern.ObjectValue is not null)
        {
            string key = PolarDbCompositeKey.Create(
                pattern.Predicate,
                ((int)pattern.ObjectKind.Value).ToString(CultureInfo.InvariantCulture),
                pattern.ObjectValue);
            return sets.Triples.Find(row => row.PredicateObjectKey, key);
        }

        if (pattern.Subject is not null)
        {
            return sets.Triples.Find(row => row.Subject, pattern.Subject);
        }

        if (pattern.Predicate is not null)
        {
            return sets.Triples.Find(row => row.Predicate, pattern.Predicate);
        }

        if (pattern.ObjectValue is not null)
        {
            return sets.Triples.Find(row => row.ObjectValue, pattern.ObjectValue);
        }

        return sets.Triples.All();
    }

    private static bool Matches(PolarDbTripleRow row, TriplePattern pattern) =>
        (pattern.Subject is null || string.Equals(row.Subject, pattern.Subject, StringComparison.Ordinal)) &&
        (pattern.Predicate is null || string.Equals(row.Predicate, pattern.Predicate, StringComparison.Ordinal)) &&
        (pattern.ObjectKind is null || row.ObjectKind == (int)pattern.ObjectKind.Value) &&
        (pattern.ObjectValue is null || string.Equals(row.ObjectValue, pattern.ObjectValue, StringComparison.Ordinal));
}
