using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogDirectiveCanonicalizer
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;

    public static FogSourceRecord Delete(
        FogSourceDescriptor source,
        long sourceOrdinal,
        XElement element,
        DateTime modifiedAt,
        string? modifiedAtRaw)
    {
        string resourceId = FogIdentifier.Require(
            element.Attribute(Rdf + "about")?.Value ?? element.Attribute("id")?.Value,
            source.FogPath,
            "delete");

        return FogDirectiveRecordFactory.Create(
            source,
            sourceOrdinal,
            FogRecordKind.Delete,
            resourceId,
            substituteTargetId: null,
            modifiedAt,
            modifiedAtRaw);
    }

    public static FogSourceRecord Substitute(
        FogSourceDescriptor source,
        long sourceOrdinal,
        XElement element,
        DateTime modifiedAt,
        string? modifiedAtRaw)
    {
        string oldId = FogIdentifier.Require(
            element.Attribute("old-id")?.Value ?? element.Attribute(Rdf + "about")?.Value,
            source.FogPath,
            "substitute old-id");
        string? newIdValue = element.Attribute("new-id")?.Value;
        if (string.IsNullOrWhiteSpace(newIdValue))
        {
            newIdValue = element.Elements()
                .FirstOrDefault(child =>
                    string.Equals(child.Name.LocalName, "newid", StringComparison.Ordinal))
                ?.Attribute(Rdf + "resource")
                ?.Value;
        }

        string newId = FogIdentifier.Require(
            newIdValue,
            source.FogPath,
            "substitute new-id");
        return FogDirectiveRecordFactory.Create(
            source,
            sourceOrdinal,
            FogRecordKind.Substitute,
            oldId,
            newId,
            modifiedAt,
            modifiedAtRaw);
    }
}
