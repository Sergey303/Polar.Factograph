using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemFogSourceScannerTests
{
    [Fact]
    public async Task ScanAsync_UsesOnlyCurrentCassetteFogFromMetaDirectory()
    {
        string configurationPath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "syp.project.json");

        ProjectConfigurationLoader loader = new();
        ProjectDefinition project = await loader.LoadAsync(configurationPath);

        CassetteDefinition cassette = Assert.Single(project.Cassettes);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "cassetes", "SypCassete")),
            cassette.Path);

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
        Assert.True(source.Length > 1_000);
        Assert.EndsWith(
            Path.Combine("meta", "SypCassete_current.fog"),
            source.FogPath);
    }
}
