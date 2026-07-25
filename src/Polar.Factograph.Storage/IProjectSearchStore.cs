namespace Polar.Factograph.Storage;

public sealed record NameSearchHit(
    string ResourceId,
    string Predicate,
    string Value,
    string? Language,
    string SourceCassetteId);

public sealed record WordSearchHit(
    string ResourceId,
    string Word,
    string Predicate,
    string Value,
    string? Language,
    string SourceCassetteId);

/// <summary>
/// Exact-key access to materialized legacy search indexes.
/// A Polar.DB.Typed implementation can satisfy every method through external-key indexes.
/// </summary>
public interface IProjectSearchStore
{
    Task<IReadOnlyList<NameSearchHit>> FindNamesByKeyAsync(
        string normalizedSearchKey,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NameSearchHit>> FindNamesByResourceAsync(
        string resourceId,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WordSearchHit>> FindWordAsync(
        string normalizedWord,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default);
}