using System.Xml.Linq;

namespace Polar.Factograph.Application;

public sealed class XmlOntologyCatalogLoader
{
    public async Task<OntologyCatalog> LoadAsync(
        string ontologyPath,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, OntologyTerm> terms = await LoadTermsAsync(
            ontologyPath,
            cancellationToken);
        return new OntologyCatalog(terms);
    }

    public async Task<IReadOnlyDictionary<string, OntologyTerm>> LoadTermsAsync(
        string ontologyPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ontologyPath);

        string fullPath = Path.GetFullPath(ontologyPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Ontology file was not found: {fullPath}",
                fullPath);
        }

        XDocument document = await OntologyXmlDocumentReader.ReadAsync(
            fullPath,
            cancellationToken);
        XElement root = document.Root
            ?? throw new InvalidDataException(
                $"Ontology XML has no root element: {fullPath}");
        return OntologyTermParser.Parse(root, fullPath);
    }
}
