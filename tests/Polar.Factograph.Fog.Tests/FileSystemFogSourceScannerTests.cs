using System.Text;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogSourceScannerTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"factograph-fog-scanner-{Guid.NewGuid():N}");

    [Fact]
    public async Task ScanAsync_UsesOnlyCurrentCassetteFogFromMetaDirectory()
    {
        string cassettePath = Path.Combine(_root, "SypCassete");
        string metaPath = Path.Combine(cassettePath, "meta");
        Directory.CreateDirectory(metaPath);
        string currentFogPath = Path.Combine(metaPath, "SypCassete_current.fog");
        await File.WriteAllTextAsync(
            currentFogPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     dbid="SypCassete_current"
                     uri="iiss://SypCassete@iis.nsk.su"
                     owner="mag_1" />
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        await File.WriteAllTextAsync(
            Path.Combine(metaPath, "SypCassete_2020.fog"),
            """
            <?xml version="1.0" encoding="utf-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     dbid="SypCassete_old" />
            """,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        ProjectDefinition project = new()
        {
            ProjectId = "test",
            Name = "Test",
            Ontology = new OntologyDefinition { Path = Path.Combine(_root, "ontology.xml") },
            Index = new IndexDefinition { Path = Path.Combine(_root, "index") },
            Cassettes =
            [
                new CassetteDefinition
                {
                    Id = "syp-cassette",
                    Name = "SypCassete",
                    Path = cassettePath,
                    Enabled = true,
                    DefaultAccess = "read",
                    AllowWrite = false
                }
            ]
        };

        FileSystemFogSourceScanner scanner = new();
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(project);

        FogSourceDescriptor source = Assert.Single(sources);
        Assert.Equal("syp-cassette", source.CassetteId);
        Assert.Equal("SypCassete", source.CassetteName);
        Assert.Equal("SypCassete_current", source.DatabaseId);
        Assert.Equal("iiss://SypCassete@iis.nsk.su", source.CassetteUri);
        Assert.Equal("mag_1", source.Owner);
        Assert.Null(source.Prefix);
        Assert.Null(source.Counter);
        Assert.False(source.Writable);
        Assert.True(source.IsCassetteMetadata);
        Assert.True(source.Length > 100);
        Assert.Equal(Path.GetFullPath(currentFogPath), source.FogPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
