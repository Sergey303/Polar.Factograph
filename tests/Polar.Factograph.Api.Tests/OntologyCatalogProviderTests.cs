using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class OntologyCatalogProviderTests
{
    [Fact]
    public async Task GetAsync_ReusesAndRefreshesCatalogByTimestamp()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-ontology-provider-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "ontology.xml");

        try
        {
            await File.WriteAllTextAsync(path, Ontology("First"));
            DateTime firstTimestamp = new(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, firstTimestamp);
            OntologyCatalogProvider provider = new(new XmlOntologyCatalogLoader());

            OntologyCatalog first = await provider.GetAsync(path);
            OntologyCatalog cached = await provider.GetAsync(path);

            Assert.Same(first, cached);
            Assert.Equal("First", first.LabelOf("person", "en"));

            await File.WriteAllTextAsync(path, Ontology("Second"));
            File.SetLastWriteTimeUtc(path, firstTimestamp.AddSeconds(2));
            OntologyCatalog refreshed = await provider.GetAsync(path);

            Assert.NotSame(first, refreshed);
            Assert.Equal("Second", refreshed.LabelOf("person", "en"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string Ontology(string label) => $$"""
        <?xml version="1.0" encoding="utf-8"?>
        <Ontology xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
          <Class rdf:about="person">
            <label xml:lang="en">{{label}}</label>
          </Class>
        </Ontology>
        """;
}
