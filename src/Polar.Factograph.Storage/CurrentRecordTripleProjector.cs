using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Storage;

public sealed record ProjectedResource(
    ResourceHead Head,
    IReadOnlyList<TripleRow> Triples);

public sealed class CurrentRecordTripleProjector
{
    private const string SystemCassetteId = "$system";
    private const string SyntheticFogPath = "$synthetic";
    private const string RdfType = LegacyFogVocabulary.RdfNamespace + "type";

    public ProjectedResource Project(FogCurrentRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        string sourceCassetteId = record.SourceCassetteId ?? SystemCassetteId;
        string sourceFogPath = record.SourceFogPath ?? SyntheticFogPath;
        string sourceOrdinal = record.SourceOrdinal?.ToString(CultureInfo.InvariantCulture) ?? "synthetic";
        Guid sourceRecordId = StableGuid(
            "source-record",
            sourceCassetteId,
            sourceFogPath,
            sourceOrdinal,
            record.ResourceId);
        DateTimeOffset modifiedAt = ToDateTimeOffset(record.ModifiedAt);

        List<TripleRow> triples = new(record.Properties.Count + 1)
        {
            CreateTriple(
                record,
                sourceRecordId,
                sourceCassetteId,
                sourceFogPath,
                modifiedAt,
                ordinal: -1,
                RdfType,
                TripleObjectKind.Iri,
                record.Type,
                language: null,
                dataType: null)
        };

        for (int index = 0; index < record.Properties.Count; index++)
        {
            FogProperty property = record.Properties[index];
            triples.Add(CreateTriple(
                record,
                sourceRecordId,
                sourceCassetteId,
                sourceFogPath,
                modifiedAt,
                index,
                property.Predicate,
                property.Kind == FogPropertyKind.Resource
                    ? TripleObjectKind.Iri
                    : TripleObjectKind.Literal,
                property.Value,
                property.Language,
                property.DataType));
        }

        ResourceHead head = new(
            record.ResourceId,
            sourceRecordId,
            sourceCassetteId,
            sourceFogPath,
            modifiedAt,
            IsDeleted: false);

        return new ProjectedResource(head, triples);
    }

    private static TripleRow CreateTriple(
        FogCurrentRecord record,
        Guid sourceRecordId,
        string sourceCassetteId,
        string sourceFogPath,
        DateTimeOffset modifiedAt,
        int ordinal,
        string predicate,
        TripleObjectKind objectKind,
        string objectValue,
        string? language,
        string? dataType)
    {
        Guid tripleId = StableGuid(
            "triple",
            sourceRecordId.ToString("N", CultureInfo.InvariantCulture),
            ordinal.ToString(CultureInfo.InvariantCulture),
            record.ResourceId,
            predicate,
            ((int)objectKind).ToString(CultureInfo.InvariantCulture),
            objectValue,
            language,
            dataType);

        return new TripleRow(
            tripleId,
            record.ResourceId,
            predicate,
            objectKind,
            objectValue,
            language,
            dataType,
            sourceRecordId,
            sourceCassetteId,
            sourceFogPath,
            modifiedAt);
    }

    private static Guid StableGuid(params string?[] parts)
    {
        string canonical = string.Join('\u001F', parts.Select(part => part ?? "\u0000"));
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return new Guid(hash.AsSpan(0, 16));
    }

    private static DateTimeOffset ToDateTimeOffset(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => new DateTimeOffset(value),
        DateTimeKind.Local => new DateTimeOffset(value),
        _ => new DateTimeOffset(value, TimeSpan.Zero)
    };
}
