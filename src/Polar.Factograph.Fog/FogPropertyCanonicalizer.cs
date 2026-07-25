using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogPropertyCanonicalizer
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;
    private static readonly XNamespace Xml = LegacyFogVocabulary.XmlNamespace;

    public static FogProperty? Canonicalize(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        if (string.Equals(element.Name.LocalName, "iisstore", StringComparison.Ordinal))
        {
            string? uri = element.Attribute("uri")?.Value;
            return string.IsNullOrWhiteSpace(uri)
                ? null
                : new FogProperty(
                    LegacyFogVocabulary.Namespace + "uri",
                    FogPropertyKind.Literal,
                    uri);
        }

        string predicate = LegacyFogVocabulary.Namespace + element.Name.LocalName;
        string? resource = element.Attribute(Rdf + "resource")?.Value;
        if (resource is not null)
        {
            return new FogProperty(
                predicate,
                FogPropertyKind.Resource,
                FogIdentifier.Clean(resource));
        }

        return new FogProperty(
            predicate,
            FogPropertyKind.Literal,
            element.Value,
            element.Attribute(Xml + "lang")?.Value,
            element.Attribute(Rdf + "datatype")?.Value);
    }
}
