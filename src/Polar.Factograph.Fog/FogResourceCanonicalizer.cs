using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogResourceCanonicalizer
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;

    public static FogSourceRecord Canonicalize(
        FogSourceDescriptor source,
        long sourceOrdinal,
        XElement element,
        DateTime modifiedAt,
        string? modifiedAtRaw)
    {
        string localName = element.Name.LocalName;
        string? about = element.Attribute(Rdf + "about")?.Value;
        string resourceId = string.IsNullOrWhiteSpace(about)
            ? FogAnonymousResourceIdentifier.Create(source, sourceOrdinal, localName)
            : FogIdentifier.Clean(about);
        List<FogProperty> properties = new();

        foreach (XElement child in element.Elements())
        {
            FogProperty? property = FogPropertyCanonicalizer.Canonicalize(child);
            if (property is not null)
            {
                properties.Add(property);
            }
        }

        return new FogSourceRecord(
            new FogRecordKey(source.FogPath, sourceOrdinal),
            source.CassetteId,
            source.CassetteName,
            FogRecordKind.Resource,
            resourceId,
            LegacyFogVocabulary.Namespace + localName,
            SubstituteTargetId: null,
            modifiedAt,
            modifiedAtRaw,
            properties);
    }
}
