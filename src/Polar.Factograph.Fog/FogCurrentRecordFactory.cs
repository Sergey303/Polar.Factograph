namespace Polar.Factograph.Fog;

internal static class FogCurrentRecordFactory
{
    public static FogCurrentRecord Create(
        FogSourceRecord record,
        FogMaterializationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(plan);

        FogProperty[] properties = record.Properties
            .Select(property => property.Kind == FogPropertyKind.Resource
                ? property with { Value = plan.ResolveReferencedId(property.Value) }
                : property)
            .ToArray();

        return new FogCurrentRecord(
            record.ResourceId,
            record.Type ?? throw new InvalidDataException(
                $"Resource '{record.ResourceId}' has no type."),
            record.ModifiedAt,
            properties,
            record.SourceCassetteId,
            record.SourceCassetteName,
            record.Key.SourceFogPath,
            record.Key.SourceOrdinal,
            IsSynthetic: false);
    }

    public static FogCurrentRecord CreateSyntheticCassetteRoot() => new(
        LegacyFogVocabulary.CassetteRootCollectionId,
        LegacyFogVocabulary.Namespace + "collection",
        DateTime.MinValue,
        new[]
        {
            new FogProperty(
                LegacyFogVocabulary.Namespace + "name",
                FogPropertyKind.Literal,
                "кассеты")
        },
        SourceCassetteId: null,
        SourceCassetteName: null,
        SourceFogPath: null,
        SourceOrdinal: null,
        IsSynthetic: true);
}
