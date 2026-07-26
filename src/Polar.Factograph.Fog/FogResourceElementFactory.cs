using System.Globalization;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogResourceElementFactory
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;
    private static readonly XNamespace Xml = LegacyFogVocabulary.XmlNamespace;

    public static XElement Create(
        FogResourceWriteRequest request,
        string resourceId,
        DateTime modifiedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(request.Properties);

        XElement resource = new(
            FogWriteXmlName.LocalName(request.TypeId),
            new XAttribute(Rdf + "about", FogIdentifier.Clean(resourceId)),
            new XAttribute(
                "mT",
                modifiedAtUtc.ToUniversalTime().ToString("u", CultureInfo.InvariantCulture)));

        foreach (FogProperty property in request.Properties)
        {
            XElement? element = CreateProperty(property);
            if (element is not null)
            {
                resource.Add(element);
            }
        }

        return resource;
    }

    private static XElement? CreateProperty(FogProperty property)
    {
        ArgumentNullException.ThrowIfNull(property);
        string name = FogWriteXmlName.LocalName(property.Predicate);

        if (property.Kind == FogPropertyKind.Resource)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(property.Value);
            return new XElement(
                name,
                new XAttribute(Rdf + "resource", FogIdentifier.Clean(property.Value)));
        }

        if (string.IsNullOrEmpty(property.Value))
        {
            return null;
        }

        return new XElement(
            name,
            string.IsNullOrWhiteSpace(property.Language)
                ? null
                : new XAttribute(Xml + "lang", property.Language),
            string.IsNullOrWhiteSpace(property.DataType)
                ? null
                : new XAttribute(Rdf + "datatype", property.DataType),
            property.Value);
    }
}
