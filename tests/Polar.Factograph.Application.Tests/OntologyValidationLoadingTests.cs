using System.Text;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class OntologyValidationLoadingTests
{
    [Fact]
    public async Task LoadTermsAsync_AllowsDiagnosticsBeforeStrictCatalogRejectsHierarchy()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-ontology-validation-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "ontology.xml");
            await File.WriteAllTextAsync(
                path,
                OntologyXml,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            XmlOntologyCatalogLoader loader = new();

            IReadOnlyDictionary<string, OntologyTerm> terms = await loader.LoadTermsAsync(path);
            OntologyValidationReport report = new OntologyValidationService()
                .Validate(terms.Values);

            Assert.Contains(report.Issues, issue =>
                issue.Code == "missing_parent_class" &&
                issue.TermId == "http://fogid.net/o/broken");
            await Assert.ThrowsAsync<KeyNotFoundException>(() => loader.LoadAsync(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private const string OntologyXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="http://fogid.net/o/sys-obj" abstract="yes">
            <label xml:lang="ru">Сущность</label>
          </Class>
          <Class rdf:about="http://fogid.net/o/broken">
            <label xml:lang="ru">Сломанный класс</label>
            <SubClassOf rdf:resource="http://fogid.net/o/missing" />
          </Class>
        </Ontology>
        """;
}
