using System.Text;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class RussianDatePresentationTests
{
    [Fact]
    public async Task Present_FormatsCompleteAndPartialDatesInRussian()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-russian-date-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string ontologyPath = Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                ontologyPath,
                OntologyXml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            OntologyCatalog catalog = await new XmlOntologyCatalogLoader()
                .LoadAsync(ontologyPath);
            OntologyResourcePortraitPresenter presenter = new(catalog);
            ProjectResourcePortrait portrait = new(
                "person-1",
                "person",
                [
                    new ResourceLiteralField(
                        "http://fogid.net/o/from-date",
                        "1983",
                        null,
                        null),
                    new ResourceLiteralField(
                        "month-date",
                        "1983-01",
                        null,
                        null),
                    new ResourceLiteralField(
                        "day-date",
                        "1983-01-07",
                        null,
                        null)
                ],
                [],
                [],
                new ResourceProvenance(
                    Guid.Empty,
                    "cassette",
                    "source.fog",
                    DateTimeOffset.UnixEpoch));

            PresentedProjectResourcePortrait result = presenter.Present(portrait, "ru");
            Dictionary<string, PresentedResourceLiteralField> fields = result.Literals
                .ToDictionary(field => field.Predicate, StringComparer.Ordinal);

            Assert.Equal("1983", fields["http://fogid.net/o/from-date"].Value);
            Assert.Equal("1983 г.", fields["http://fogid.net/o/from-date"].DisplayValue);
            Assert.Equal("Январь 1983 г.", fields["month-date"].DisplayValue);
            Assert.Equal("7 января 1983 г.", fields["day-date"].DisplayValue);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="person">
            <label xml:lang="ru">Персона</label>
          </Class>
          <DatatypeProperty rdf:about="http://fogid.net/o/from-date">
            <label xml:lang="ru">Начальная дата</label>
            <domain rdf:resource="person" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="month-date">
            <label xml:lang="ru">Месяц</label>
            <domain rdf:resource="person" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
          <DatatypeProperty rdf:about="day-date">
            <label xml:lang="ru">Дата</label>
            <domain rdf:resource="person" />
            <range rdf:resource="http://fogid.net/o/date" />
          </DatatypeProperty>
        </Ontology>
        """;
}
