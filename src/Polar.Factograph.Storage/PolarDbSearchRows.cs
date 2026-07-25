namespace Polar.Factograph.Storage;

/// <summary>
/// One exact lookup key for legacy name/alias prefix search.
/// Multiple rows may point to the same source literal.
/// </summary>
public sealed record PolarDbNameSearchRow(
    Guid SearchRowId,
    string SearchKey,
    string ResourceId,
    string Predicate,
    string Value,
    string Language,
    string SourceCassetteId);

/// <summary>
/// One normalized word from a legacy searchable literal.
/// </summary>
public sealed record PolarDbWordSearchRow(
    Guid SearchRowId,
    string Word,
    string ResourceId,
    string Predicate,
    string Value,
    string Language,
    string SourceCassetteId);

public static class PolarDbSearchPhysicalSchema
{
    public static PolarDbSetDefinition NameSearch { get; } = new(
        StorageName: "name-search",
        PrimaryKeyField: nameof(PolarDbNameSearchRow.SearchRowId),
        ExternalKeyFields:
        [
            nameof(PolarDbNameSearchRow.SearchKey),
            nameof(PolarDbNameSearchRow.ResourceId),
            nameof(PolarDbNameSearchRow.SourceCassetteId)
        ]);

    public static PolarDbSetDefinition WordSearch { get; } = new(
        StorageName: "word-search",
        PrimaryKeyField: nameof(PolarDbWordSearchRow.SearchRowId),
        ExternalKeyFields:
        [
            nameof(PolarDbWordSearchRow.Word),
            nameof(PolarDbWordSearchRow.ResourceId),
            nameof(PolarDbWordSearchRow.SourceCassetteId)
        ]);
}