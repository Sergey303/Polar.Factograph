using System.Text;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Tests;

internal static class OntologyWriteTestCatalog
{
    public static async Task<OntologyCatalog> CreateAsync()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-write-ontology-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "ontology.xml");
        try
        {
            await File.WriteAllTextAsync(path, Xml, new UTF8Encoding(false));
            return await new XmlOntologyCatalogLoader().LoadAsync(path);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="base" />
          <Class rdf:about="child"><SubClassOf rdf:resource="base" /></Class>
          <Class rdf:about="other" />
          <DatatypeProperty rdf:about="name">
            <domain rdf:resource="base" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="mentor">
            <domain rdf:resource="child" />
            <range rdf:resource="base" />
          </ObjectProperty>
          <EnumerationType rdf:about="status-enum">
            <state value="active">Active</state>
            <state value="inactive">Inactive</state>
          </EnumerationType>
          <DatatypeProperty rdf:about="status">
            <domain rdf:resource="child" />
            <range rdf:resource="status-enum" />
          </DatatypeProperty>
        </Ontology>
        """;
}
