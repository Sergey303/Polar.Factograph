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
            Assert.False(baseClass.IsEntityType);

            OntologyWriteClassResponse child = Assert.Single(
                schema.Classes,
                item => item.Id == "child");
            Assert.Equal("Дочерний класс", child.Label);
            Assert.Equal("base", child.ParentClassId);
            Assert.False(child.IsAbstract);
            Assert.True(child.IsEntityType);
            Assert.Equal(new[] { "name", "mentor", "status" },
                child.Properties.Select(item => item.Id));

            OntologyWriteClassResponse participation = Assert.Single(
                schema.Classes,
                item => item.Id == "participation");
            Assert.False(participation.IsAbstract);
            Assert.False(participation.IsEntityType);

            OntologyWritePropertyResponse mentor = Assert.Single(
                child.Properties,
                item => item.Id == "mentor");
            Assert.Equal("resource", mentor.Kind);
            Assert.Equal("Ученики", mentor.InverseLabel);
            Assert.True(mentor.IsEssential);
            Assert.Equal(new[] { "child" }, mentor.Ranges);

            OntologyWritePropertyResponse name = Assert.Single(
                child.Properties,
                item => item.Id == "name");
            Assert.False(name.IsEssential);
            Assert.Null(name.InverseLabel);

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
          <Class rdf:about="http://fogid.net/o/entity" abstract="yes" />
          <Class rdf:about="http://fogid.net/o/sys-obj" abstract="yes">
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <Class rdf:about="base" abstract="yes">
            <label xml:lang="ru">Базовый класс</label>
            <SubClassOf rdf:resource="http://fogid.net/o/sys-obj" />
          </Class>
          <Class rdf:about="child">
            <label xml:lang="ru">Дочерний класс</label>
            <SubClassOf rdf:resource="base" />
          </Class>
          <Class rdf:about="participation">
            <label xml:lang="ru">Участие</label>
            <SubClassOf rdf:resource="http://fogid.net/o/entity" />
          </Class>
          <DatatypeProperty rdf:about="name" priority="01">
            <label xml:lang="ru">Имя</label><domain rdf:resource="base" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="mentor" priority="02" essential="yes">
            <label xml:lang="ru">Наставник</label>
            <inverse-label xml:lang="ru">Ученики</inverse-label>
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
