using System.Globalization;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogDirectiveElementFactory
{
    private static readonly XNamespace Fog = LegacyFogVocabulary.Namespace;
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;

    public static XElement Create(
        FogDirectiveWriteRequest request,
        DateTime modifiedAtUtc)
    {
        string resourceId = FogIdentifier.Clean(request.ResourceId);
        XAttribute timestamp = new(
            "mT",
            modifiedAtUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture));

        if (request.Kind == FogRecordKind.Delete)
        {
            return new XElement(
                Fog + "delete",
                new XAttribute(Rdf + "about", resourceId),
                timestamp);
        }

        return new XElement(
            Fog + "substitute",
            new XAttribute("old-id", resourceId),
            new XAttribute(
                "new-id",
                FogIdentifier.Clean(request.SubstituteTargetId!)),
            timestamp);
    }
}
