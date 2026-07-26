namespace Polar.Factograph.Api.Tests;

internal static class WritableApiProjectFog
{
    public const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                 dbid="write-test" owner="editor" prefix="p" counter="1">
          <person rdf:about="existing" mT="2020-01-01 00:00:00Z">
            <name>Existing</name>
          </person>
          <person rdf:about="target" mT="2020-01-01 00:00:01Z">
            <name>Target</name>
          </person>
          <organization rdf:about="company" mT="2020-01-01 00:00:02Z" />
          <collection rdf:about="collection-1" mT="2020-01-01 00:00:03Z" />
        </rdf:RDF>
        """;
}
