using System.Text;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class XmlOntologyCatalogTests
{
    [Fact]
    public async Task LoadAsync_ProvidesLabelsHierarchyPropertiesAndEnumerationValues()
    {
        await using TemporaryOntology ontology = await TemporaryOntology.CreateAsync(ValidOntologyXml);
        XmlOntologyCatalogLoader loader = new();

        OntologyCatalog catalog = await loader.LoadAsync(ontology.Path);

        Assert.Equal("Базовый класс", catalog.LabelOf("base", "ru"));
        Assert.Equal("Child", catalog.LabelOf("child", "ru"));
        Assert.Equal("Ученики", catalog.InverseLabelOf("mentor", "ru"));
        Assert.Equal(new[] { "base", "child" }, catalog.AncestorsAndSelf("child"));
        Assert.Equal(
            new[] { "name", "mentor", "status" },
            catalog.DirectPropertiesForType("child").Select(term => term.Id));
        Assert.Equal(
            new[] { "mentor" },
            catalog.InversePropertiesForType("child").Select(term => term.Id));
        Assert.Equal("Активен", catalog.EnumerationLabel("status", "active", "ru"));
        Assert.Equal("Активен", catalog.EnumerationLabel("status", "active", "de"));
        Assert.Null(catalog.LabelOf("unknown", "ru"));
    }

    [Fact]
    public async Task LoadAsync_RejectsCyclicClassHierarchy()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
              <Class rdf:about="a"><SubClassOf rdf:resource="b" /></Class>
              <Class rdf:about="b"><SubClassOf rdf:resource="a" /></Class>
            </Ontology>
            """;
        await using TemporaryOntology ontology = await TemporaryOntology.CreateAsync(xml);
        XmlOntologyCatalogLoader loader = new();

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => loader.LoadAsync(ontology.Path));

        Assert.Contains("Cyclic ontology class hierarchy", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_RejectsDuplicateOntologyIdentifiers()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
              <Class rdf:about="duplicate" />
              <DatatypeProperty rdf:about="duplicate" />
            </Ontology>
            """;
        await using TemporaryOntology ontology = await TemporaryOntology.CreateAsync(xml);
        XmlOntologyCatalogLoader loader = new();

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => loader.LoadAsync(ontology.Path));

        Assert.Contains("Duplicate ontology identifier", exception.Message, StringComparison.Ordinal);
    }

    private const string ValidOntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="base">
            <label xml:lang="ru">Базовый класс</label>
          </Class>
          <Class rdf:about="child">
            <label xml:lang="en">Child</label>
            <SubClassOf rdf:resource="base" />
          </Class>
          <DatatypeProperty rdf:about="name" priority="01">
            <label xml:lang="ru">Имя</label>
            <domain rdf:resource="base" />
          </DatatypeProperty>
          <ObjectProperty rdf:about="mentor" priority="02">
            <label xml:lang="ru">Наставник</label>
            <inverse-label xml:lang="ru">Ученики</inverse-label>
            <domain rdf:resource="base" />
            <range rdf:resource="child" />
          </ObjectProperty>
          <EnumerationType rdf:about="status-enum">
            <state value="active" xml:lang="ru">Активен</state>
            <state value="active" xml:lang="en">Active</state>
          </EnumerationType>
          <DatatypeProperty rdf:about="status" priority="03">
            <label xml:lang="ru">Статус</label>
            <domain rdf:resource="child" />
            <range rdf:resource="status-enum" />
          </DatatypeProperty>
        </Ontology>
        """;

    private sealed class TemporaryOntology : IAsyncDisposable
    {
        private TemporaryOntology(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        public string Directory { get; }
        public string Path { get; }

        public static async Task<TemporaryOntology> CreateAsync(string content)
        {
            string directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "polar-factograph-ontology-tests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                path,
                content,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return new TemporaryOntology(directory, path);
        }

        public ValueTask DisposeAsync()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
