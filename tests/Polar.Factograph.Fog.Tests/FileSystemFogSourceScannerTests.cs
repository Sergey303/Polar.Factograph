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
        await WriteFogAsync(
            currentFogPath,
            "SypCassete_current",
            "iiss://SypCassete@iis.nsk.su",
            "mag_1");
        await WriteFogAsync(
            Path.Combine(metaPath, "SypCassete_2020.fog"),
            "SypCassete_old");

        FileSystemFogSourceScanner scanner = new();
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(
            CreateProject("syp-cassette", "SypCassete", cassettePath));

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

    [Fact]
    public async Task ScanAsync_UsesSingleCurrentFogWhenCassetteFolderWasRenamed()
    {
        string cassettePath = Path.Combine(_root, "Syp2023a");
        string metaPath = Path.Combine(cassettePath, "meta");
        Directory.CreateDirectory(metaPath);
        string legacyCurrentFogPath = Path.Combine(metaPath, "Syp2023_current.fog");
        await WriteFogAsync(
            legacyCurrentFogPath,
            "Syp2023_current",
            "iiss://Syp2023@iis.nsk.su");

        FileSystemFogSourceScanner scanner = new();
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(
            CreateProject("Syp2023a", "Syp2023a", cassettePath));

        FogSourceDescriptor source = Assert.Single(sources);
        Assert.Equal(Path.GetFullPath(legacyCurrentFogPath), source.FogPath);
        Assert.Equal("Syp2023_current", source.DatabaseId);
    }

    [Fact]
    public async Task ScanAsync_UsesOnlyFogWhenLegacyMetaHasNoCurrentSuffix()
    {
        string cassettePath = Path.Combine(_root, "LegacyCassette");
        string metaPath = Path.Combine(cassettePath, "meta");
        Directory.CreateDirectory(metaPath);
        string onlyFogPath = Path.Combine(metaPath, "legacy-metadata.fog");
        await WriteFogAsync(onlyFogPath, "legacy-metadata");

        FileSystemFogSourceScanner scanner = new();
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(
            CreateProject("LegacyCassette", "LegacyCassette", cassettePath));

        Assert.Equal(Path.GetFullPath(onlyFogPath), Assert.Single(sources).FogPath);
    }

    [Fact]
    public async Task ScanAsync_RejectsSeveralUnmatchedCurrentFogFiles()
    {
        string cassettePath = Path.Combine(_root, "AmbiguousCassette");
        string metaPath = Path.Combine(cassettePath, "meta");
        Directory.CreateDirectory(metaPath);
        await WriteFogAsync(Path.Combine(metaPath, "First_current.fog"), "first");
        await WriteFogAsync(Path.Combine(metaPath, "Second_current.fog"), "second");

        FileSystemFogSourceScanner scanner = new();
        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => scanner.ScanAsync(
                CreateProject("AmbiguousCassette", "AmbiguousCassette", cassettePath)));

        Assert.Contains("several *_current.fog files", exception.Message, StringComparison.Ordinal);
        Assert.Contains("First_current.fog", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Second_current.fog", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private ProjectDefinition CreateProject(
        string cassetteId,
        string cassetteName,
        string cassettePath) => new()
    {
        ProjectId = "test",
        Name = "Test",
        Ontology = new OntologyDefinition { Path = Path.Combine(_root, "ontology.xml") },
        Index = new IndexDefinition { Path = Path.Combine(_root, "index") },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = cassetteId,
                Name = cassetteName,
                Path = cassettePath,
                Enabled = true,
                DefaultAccess = "read",
                AllowWrite = false
            }
        ]
    };

    private static Task WriteFogAsync(
        string path,
        string databaseId,
        string? cassetteUri = null,
        string? owner = null)
    {
        string uriAttribute = cassetteUri is null ? string.Empty : $" uri=\"{cassetteUri}\"";
        string ownerAttribute = owner is null ? string.Empty : $" owner=\"{owner}\"";
        string xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                     dbid="{databaseId}"{uriAttribute}{ownerAttribute} />
            """;
        return File.WriteAllTextAsync(
            path,
            xml,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
