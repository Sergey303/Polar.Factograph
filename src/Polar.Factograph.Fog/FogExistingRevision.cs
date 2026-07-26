using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogExistingRevision
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;

    public static DateTime? ModifiedAt(
        XElement element,
        string resourceId,
        string fogPath)
    {
        string localName = element.Name.LocalName;
        if (string.Equals(localName, "delete", StringComparison.Ordinal) ||
            string.Equals(localName, "substitute", StringComparison.Ordinal))
        {
            return null;
        }

        string? existingId = element.Attribute(Rdf + "about")?.Value;
        if (existingId is null ||
            !string.Equals(
                FogIdentifier.Clean(existingId),
                resourceId,
                StringComparison.Ordinal))
        {
            return null;
        }

        return LegacyFogTime.Parse(
            element.Attribute("mT")?.Value,
            fogPath);
    }
}
