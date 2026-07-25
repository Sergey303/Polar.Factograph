using System.Xml.Linq;

namespace Polar.Factograph.Application;

internal static class OntologyXmlValueReader
{
    private static readonly XNamespace Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    private static readonly XNamespace Xml = "http://www.w3.org/XML/1998/namespace";

    public static OntologyTermKind? ParseKind(string localName) => localName switch
    {
        "Class" => OntologyTermKind.Class,
        "DatatypeProperty" => OntologyTermKind.DatatypeProperty,
        "ObjectProperty" => OntologyTermKind.ObjectProperty,
        "EnumerationType" => OntologyTermKind.EnumerationType,
        _ => null
    };

    public static string? ReadAbout(XElement element) =>
        element.Attribute(Rdf + "about")?.Value;

    public static string? ReadFirstResource(
        XElement element,
        string localName) => element.Elements()
        .FirstOrDefault(child =>
            string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
        ?.Attribute(Rdf + "resource")
        ?.Value;

    public static OntologyLocalizedText[] ReadLocalized(
        XElement element,
        string localName) => element.Elements()
        .Where(child => string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
        .Select(child => new OntologyLocalizedText(
            child.Value,
            child.Attribute(Xml + "lang")?.Value))
        .ToArray();

    public static string[] ReadResources(
        XElement element,
        string localName) => element.Elements()
        .Where(child => string.Equals(child.Name.LocalName, localName, StringComparison.Ordinal))
        .Select(child => child.Attribute(Rdf + "resource")?.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value!)
        .Distinct(StringComparer.Ordinal)
        .ToArray();

    public static OntologyEnumerationState[] ReadEnumerationStates(XElement element) =>
        element.Elements()
            .Where(child => string.Equals(child.Name.LocalName, "state", StringComparison.Ordinal))
            .Select(child => new
            {
                Value = child.Attribute("value")?.Value,
                Label = child.Value,
                Language = child.Attribute(Xml + "lang")?.Value
            })
            .Where(state => !string.IsNullOrWhiteSpace(state.Value))
            .Select(state => new OntologyEnumerationState(
                state.Value!,
                state.Label,
                state.Language))
            .ToArray();
}
