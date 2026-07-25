using System.Text;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class OntologyResourcePortraitPresenterTests
{
    [Fact]
    public async Task Present_LabelsTranslatesAndOrdersPortraitFields()
    {
        await using TemporaryOntology ontology = await TemporaryOntology.CreateAsync(OntologyXml);
        OntologyCatalog catalog = await new XmlOntologyCatalogLoader().LoadAsync(ontology.Path);
        OntologyResourcePortraitPresenter presenter = new(catalog);
        ResourceProvenance provenance = new(
            Guid.Parse("10000000-0000-0000-0000-000000000001"),
            "cassette-a",
            "meta/current.fog",
            new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.Zero));
        ProjectResourcePortrait portrait = new(
            "resource-1",
            "child",
            [
                new ResourceLiteralField("unknown", "z", null, null),
                new ResourceLiteralField("status", "active", "ru", null),
                new ResourceLiteralField("name", "Иван", "ru", null)
            ],
            [
                new ResourceDirectLink("unknown-link", "resource-9"),
                new ResourceDirectLink("mentor", "resource-2")
            ],
            [
                new ResourceInverseLink("unknown-inverse", "resource-8", "cassette-b"),
                new ResourceInverseLink("mentor", "resource-3", "cassette-a")
            ],
            provenance);

        PresentedProjectResourcePortrait result = presenter.Present(portrait, "ru");

        Assert.Equal("Ребёнок", result.TypeLabel);
        Assert.Equal(new[] { "name", "status", "unknown" }, result.Literals.Select(field => field.Predicate));
        Assert.Equal("Имя", result.Literals[0].Label);
        Assert.Equal("Иван", result.Literals[0].DisplayValue);
        Assert.Equal("Статус", result.Literals[1].Label);
        Assert.Equal("active", result.Literals[1].Value);
        Assert.Equal("Активен", result.Literals[1].DisplayValue);
        Assert.Equal("unknown", result.Literals[2].Label);
        Assert.Equal(new[] { "mentor", "unknown-link" }, result.DirectLinks.Select(link => link.Predicate));
        Assert.Equal("Наставник", result.DirectLinks[0].Label);
        Assert.Equal(new[] { "mentor", "unknown-inverse" }, result.InverseLinks.Select(link => link.Predicate));
        Assert.Equal("Ученики", result.InverseLinks[0].Label);
        Assert.Same(provenance, result.Provenance);
    }

    [Fact]
    public async Task Present_UsesStableIdentifiersWhenOntologyDoesNotKnowTheTypeOrProperty()
    {
        await using TemporaryOntology ontology = await TemporaryOntology.CreateAsync(OntologyXml);
        OntologyCatalog catalog = await new XmlOntologyCatalogLoader().LoadAsync(ontology.Path);
        OntologyResourcePortraitPresenter presenter = new(catalog);
        ProjectResourcePortrait portrait = new(
            "resource-unknown",
            "unknown-type",
            [new ResourceLiteralField("unknown-property", "raw", null, null)],
            [],
            [],
            new ResourceProvenance(
                Guid.Empty,
                "cassette-a",
                "meta/current.fog",
                DateTimeOffset.UnixEpoch));

        PresentedProjectResourcePortrait result = presenter.Present(portrait, "ru");

        Assert.Equal("unknown-type", result.TypeLabel);
        PresentedResourceLiteralField field = Assert.Single(result.Literals);
        Assert.Equal("unknown-property", field.Label);
        Assert.Equal("raw", field.DisplayValue);
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="base">
            <label xml:lang="ru">Базовый класс</label>
          </Class>
          <Class rdf:about="child">
            <label xml:lang="ru">Ребёнок</label>
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
                "polar-factograph-presenter-tests",
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