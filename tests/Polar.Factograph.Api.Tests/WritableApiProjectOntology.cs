namespace Polar.Factograph.Api.Tests;

internal static class WritableApiProjectOntology
{
    public const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="agent" />
          <Class rdf:about="person">
            <SubClassOf rdf:resource="agent" />
          </Class>
          <Class rdf:about="organization" />
          <Class rdf:about="collection" />
          <Class rdf:about="collection-member" />
          <DatatypeProperty rdf:about="name">
            <domain rdf:resource="person" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="mentor">
            <domain rdf:resource="person" />
            <range rdf:resource="agent" />
          </ObjectProperty>
          <ObjectProperty rdf:about="employer">
            <domain rdf:resource="person" />
            <range rdf:resource="organization" />
          </ObjectProperty>
          <ObjectProperty rdf:about="in-collection">
            <domain rdf:resource="collection-member" />
            <range rdf:resource="collection" />
          </ObjectProperty>
          <ObjectProperty rdf:about="collection-item">
            <domain rdf:resource="collection-member" />
            <range rdf:resource="agent" />
          </ObjectProperty>
        </Ontology>
        """;
}
