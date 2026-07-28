using System.Xml.Linq;

namespace Polar.Factograph.Application;

internal static class OntologyTermParser
{
    public static IReadOnlyDictionary<string, OntologyTerm> Parse(
        XElement root,
        string fullPath)
    {
        Dictionary<string, OntologyTerm> terms = new(StringComparer.Ordinal);

        foreach (XElement element in root.Elements())
        {
            OntologyTermKind? kind = OntologyXmlValueReader.ParseKind(element.Name.LocalName);
            if (kind is null)
            {
                continue;
            }

            string id = OntologyXmlValueReader.ReadAbout(element)
                ?? throw new InvalidDataException(
                    $"Ontology {element.Name.LocalName} has no rdf:about: {fullPath}");
            OntologyTerm term = new(
                id,
                kind.Value,
                OntologyXmlValueReader.ReadLocalized(element, "label"),
                OntologyXmlValueReader.ReadLocalized(element, "inverse-label"),
                element.Attribute("priority")?.Value,
                OntologyXmlValueReader.ReadFirstResource(element, "SubClassOf"),
                OntologyXmlValueReader.ReadResources(element, "domain"),
                OntologyXmlValueReader.ReadResources(element, "range"),
                OntologyXmlValueReader.ReadEnumerationStates(element))
            {
                IsAbstract = kind == OntologyTermKind.Class &&
                    IsEnabledFlag(element.Attribute("abstract")?.Value)
            };

            if (!terms.TryAdd(id, term))
            {
                throw new InvalidDataException(
                    $"Duplicate ontology identifier '{id}': {fullPath}");
            }
        }

        return terms;
    }

    private static bool IsEnabledFlag(string? value) =>
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "1", StringComparison.Ordinal);
}
