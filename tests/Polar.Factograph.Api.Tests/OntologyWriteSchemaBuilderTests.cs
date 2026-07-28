using System.Text;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyWriteSchemaBuilderTests
{
    [Fact]
    public async Task Build_ProjectsInheritedPropertiesKindsAndEnumerationOptions()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ontology-{Guid.NewGuid():N}.xml");
        try
        {
            await File.WriteAllTextAsync(path, OntologyXml, new UTF8Encoding(false));
            OntologyCatalog catalog = await new XmlOntologyCatalogLoader().LoadAsync(path);

            OntologyWriteSchemaResponse schema = new OntologyWriteSchemaBuilder()
                .Build(catalog, "ru");

            OntologyWriteClassResponse baseClass = Assert.Single(
                schema.Classes,
                item => item.Id == "base");
            Assert.True(baseClass.IsAbstract);

            OntologyWriteClassResponse child = Assert.Single(
                schema.Classes,
                item => item.Id == "child");
            Assert.Equal("Дочерний класс", child.Label);
            Assert.Equal("base", child.ParentClassId);
            Assert.False(child.IsAbstract);
            Assert.Equal(new[] { "name", "mentor", "status" },
                child.Properties.Select(item => item.Id));

            OntologyWritePropertyResponse mentor = Assert.Single(
                child.Properties,
                item => item.Id == "mentor");
            Assert.Equal("resource", mentor.Kind);
            Assert.Equal(new[] { "child" }, mentor.Ranges);

            OntologyWritePropertyResponse status = Assert.Single(
                child.Properties,
                item => item.Id == "status");
            Assert.Equal("literal", status.Kind);
            OntologyWriteOptionResponse option = Assert.Single(status.Options);
            Assert.Equal("active", option.Value);
            Assert.Equal("Активен", option.Label);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="base" abstract="yes"><label xml:lang="ru">Базовый класс</label></Class>
          <Class rdf:about="child">
            <label xml:lang="ru">Дочерний класс</label>
            <SubClassOf rdf:resource="base" />
          </Class>
          <DatatypeProperty rdf:about="name" priority="01">
            <label xml:lang="ru">Имя</label><domain rdf:resource="base" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="mentor" priority="02">
            <label xml:lang="ru">Наставник</label>
            <domain rdf:resource="base" /><range rdf:resource="child" />
          </ObjectProperty>
          <EnumerationType rdf:about="status-enum">
            <state value="active" xml:lang="ru">Активен</state>
            <state value="active" xml:lang="en">Active</state>
          </EnumerationType>
          <DatatypeProperty rdf:about="status" priority="03">
            <label xml:lang="ru">Статус</label>
            <domain rdf:resource="child" /><range rdf:resource="status-enum" />
          </DatatypeProperty>
        </Ontology>
        """;
}
