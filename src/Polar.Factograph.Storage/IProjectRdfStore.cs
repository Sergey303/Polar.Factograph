namespace Polar.Factograph.Storage;

public enum TripleObjectKind
{
    Iri = 1,
    Literal = 2,
    BlankNode = 3
}

public sealed record TripleRow(
    Guid TripleId,
    string Subject,
    string Predicate,
    TripleObjectKind ObjectKind,
    string ObjectValue,
    string? Language,
    string? DataType,
    Guid SourceRecordId,
    string SourceCassetteId,
    string SourceFogPath,
    DateTimeOffset ModifiedAt);

public sealed record ResourceHead(
    string ResourceId,
    Guid CurrentSourceRecordId,
    string SourceCassetteId,
    string SourceFogPath,
    DateTimeOffset ModifiedAt,
    bool IsDeleted);

public sealed record TriplePattern(
    string? Subject = null,
    string? Predicate = null,
    TripleObjectKind? ObjectKind = null,
    string? ObjectValue = null);

public interface IProjectRdfStore
{
    ValueTask<ResourceHead?> GetResourceHeadAsync(
        string resourceId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<TripleRow> FindAsync(
        TriplePattern pattern,
        IReadOnlySet<string> allowedCassetteIds,
        CancellationToken cancellationToken = default);

    Task RebuildAsync(CancellationToken cancellationToken = default);
}
