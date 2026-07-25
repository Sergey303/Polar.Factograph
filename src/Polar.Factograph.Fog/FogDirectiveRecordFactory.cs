namespace Polar.Factograph.Fog;

internal static class FogDirectiveRecordFactory
{
    public static FogSourceRecord Create(
        FogSourceDescriptor source,
        long sourceOrdinal,
        FogRecordKind kind,
        string resourceId,
        string? substituteTargetId,
        DateTime modifiedAt,
        string? modifiedAtRaw) => new(
        new FogRecordKey(source.FogPath, sourceOrdinal),
        source.CassetteId,
        source.CassetteName,
        kind,
        resourceId,
        Type: null,
        substituteTargetId,
        modifiedAt,
        modifiedAtRaw,
        Array.Empty<FogProperty>());
}
