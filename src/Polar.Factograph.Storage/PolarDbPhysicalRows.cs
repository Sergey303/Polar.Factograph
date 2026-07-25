using System.Globalization;

namespace Polar.Factograph.Storage;

/// <summary>
/// Physical resource-head row restricted to CLR types supported automatically by Polar.DB.Typed DbSet.
/// </summary>
public sealed record PolarDbResourceHeadRow(
    string ResourceId,
    Guid CurrentSourceRecordId,
    string SourceCassetteId,
    string SourceFogPath,
    long ModifiedAtUtcTicks,
    bool IsDeleted);

/// <summary>
/// Physical RDF triple row restricted to CLR types supported automatically by Polar.DB.Typed DbSet.
/// Empty Language and DataType values mean that the logical nullable value is absent.
/// </summary>
public sealed record PolarDbTripleRow(
    Guid TripleId,
    string Subject,
    string Predicate,
    int ObjectKind,
    string ObjectValue,
    string Language,
    string DataType,
    Guid SourceRecordId,
    string SourceCassetteId,
    string SourceFogPath,
    long ModifiedAtUtcTicks,
    string SubjectPredicateKey,
    string PredicateObjectKey);

public sealed record PolarDbSetDefinition(
    string StorageName,
    string PrimaryKeyField,
    IReadOnlyList<string> ExternalKeyFields);

/// <summary>
/// Stable physical table and index contract for the future Polar.DB.Typed adapter.
/// Composite indexes are represented by collision-free synthetic string fields because DbSet indexes one field at a time.
/// </summary>
public static class PolarDbPhysicalSchema
{
    public static PolarDbSetDefinition ResourceHeads { get; } = new(
        StorageName: "resource-heads",
        PrimaryKeyField: nameof(PolarDbResourceHeadRow.ResourceId),
        ExternalKeyFields:
        [
            nameof(PolarDbResourceHeadRow.SourceCassetteId)
        ]);

    public static PolarDbSetDefinition Triples { get; } = new(
        StorageName: "triples",
        PrimaryKeyField: nameof(PolarDbTripleRow.TripleId),
        ExternalKeyFields:
        [
            nameof(PolarDbTripleRow.Subject),
            nameof(PolarDbTripleRow.Predicate),
            nameof(PolarDbTripleRow.ObjectValue),
            nameof(PolarDbTripleRow.SourceCassetteId),
            nameof(PolarDbTripleRow.SubjectPredicateKey),
            nameof(PolarDbTripleRow.PredicateObjectKey)
        ]);
}

public static class PolarDbRowMapper
{
    public static PolarDbResourceHeadRow ToPhysical(ResourceHead row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new PolarDbResourceHeadRow(
            row.ResourceId,
            row.CurrentSourceRecordId,
            row.SourceCassetteId,
            row.SourceFogPath,
            ToUtcTicks(row.ModifiedAt),
            row.IsDeleted);
    }

    public static ResourceHead ToLogical(PolarDbResourceHeadRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new ResourceHead(
            row.ResourceId,
            row.CurrentSourceRecordId,
            row.SourceCassetteId,
            row.SourceFogPath,
            FromUtcTicks(row.ModifiedAtUtcTicks),
            row.IsDeleted);
    }

    public static PolarDbTripleRow ToPhysical(TripleRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new PolarDbTripleRow(
            row.TripleId,
            row.Subject,
            row.Predicate,
            (int)row.ObjectKind,
            row.ObjectValue,
            row.Language ?? string.Empty,
            row.DataType ?? string.Empty,
            row.SourceRecordId,
            row.SourceCassetteId,
            row.SourceFogPath,
            ToUtcTicks(row.ModifiedAt),
            PolarDbCompositeKey.Create(row.Subject, row.Predicate),
            PolarDbCompositeKey.Create(
                row.Predicate,
                ((int)row.ObjectKind).ToString(CultureInfo.InvariantCulture),
                row.ObjectValue));
    }

    public static TripleRow ToLogical(PolarDbTripleRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!Enum.IsDefined(typeof(TripleObjectKind), row.ObjectKind))
        {
            throw new InvalidDataException(
                $"Unknown stored RDF object kind '{row.ObjectKind}' for triple '{row.TripleId}'.");
        }

        return new TripleRow(
            row.TripleId,
            row.Subject,
            row.Predicate,
            (TripleObjectKind)row.ObjectKind,
            row.ObjectValue,
            EmptyToNull(row.Language),
            EmptyToNull(row.DataType),
            row.SourceRecordId,
            row.SourceCassetteId,
            row.SourceFogPath,
            FromUtcTicks(row.ModifiedAtUtcTicks));
    }

    private static long ToUtcTicks(DateTimeOffset value) => value.UtcDateTime.Ticks;

    private static DateTimeOffset FromUtcTicks(long value) =>
        new(new DateTime(value, DateTimeKind.Utc));

    private static string? EmptyToNull(string value) =>
        string.IsNullOrEmpty(value) ? null : value;
}

public static class PolarDbCompositeKey
{
    private const char Separator = ':';

    public static string Create(params string[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        return string.Concat(parts.Select(EncodePart));
    }

    private static string EncodePart(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Length.ToString(CultureInfo.InvariantCulture) + Separator + value;
    }
}
