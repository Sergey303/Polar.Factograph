using System.Text;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FogAnonymousResourceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "polar-factograph-anonymous-fog-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Reader_assigns_stable_unique_ids_and_ignores_empty_delete_marker()
    {
        string fogPath = Path.Combine(
            _root,
            "PA_Users",
            "originals",
            "0001",
            "0002.fog");
        Directory.CreateDirectory(Path.GetDirectoryName(fogPath)!);
        await File.WriteAllTextAsync(fogPath, ReflectionFog, new UTF8Encoding(false));

        FogSourceDescriptor source = Source(fogPath);
        FileSystemFogRecordReader reader = new();

        FogSourceRecord[] firstRead = await ReadAllAsync(reader.ReadAsync(source));
        FogSourceRecord[] secondRead = await ReadAllAsync(reader.ReadAsync(source));

        Assert.Equal(2, firstRead.Length);
        Assert.Equal(
            firstRead.Select(record => record.ResourceId),
            secondRead.Select(record => record.ResourceId));
        Assert.All(firstRead, record =>
        {
            Assert.Equal(FogRecordKind.Resource, record.Kind);
            Assert.Equal("http://fogid.net/o/reflection", record.Type);
            Assert.True(record.ResourceId.StartsWith(
                "urn:polar-factograph:anonymous:",
                StringComparison.Ordinal));
        });
        Assert.NotEqual(firstRead[0].ResourceId, firstRead[1].ResourceId);
        Assert.Equal("person-1", Assert.Single(firstRead[0].Properties).Value);
        Assert.Equal("person-2", Assert.Single(firstRead[1].Properties).Value);
    }

    [Fact]
    public async Task Reader_ignores_anonymous_delete_directive()
    {
        string fogPath = Path.Combine(_root, "PA_Users", "originals", "0001", "delete.fog");
        Directory.CreateDirectory(Path.GetDirectoryName(fogPath)!);
        await File.WriteAllTextAsync(fogPath, AnonymousDeleteFog, new UTF8Encoding(false));

        FogSourceRecord[] records = await ReadAllAsync(
            new FileSystemFogRecordReader().ReadAsync(Source(fogPath)));

        Assert.Empty(records);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static FogSourceDescriptor Source(string fogPath) => new(
        CassetteId: "PA_Users",
        CassetteName: "PA_Users",
        FogPath: fogPath,
        DatabaseId: "PA_Users_0002",
        CassetteUri: null,
        Owner: "test",
        Prefix: null,
        Counter: null,
        Writable: false,
        IsCassetteMetadata: false,
        Length: new FileInfo(fogPath).Length,
        LastWriteTimeUtc: File.GetLastWriteTimeUtc(fogPath));

    private static async Task<FogSourceRecord[]> ReadAllAsync(
        IAsyncEnumerable<FogSourceRecord> records)
    {
        List<FogSourceRecord> result = [];
        await foreach (FogSourceRecord record in records)
        {
            result.Add(record);
        }

        return result.ToArray();
    }

    private const string ReflectionFog = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF
          xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
          xmlns:fog="http://fogid.net/o/"
          dbid="PA_Users_0002"
          owner="test">
          <fog:reflection mT="2024-01-01T00:00:00">
            <fog:reflected rdf:resource="person-1" />
          </fog:reflection>
          <fog:delete />
          <fog:reflection>
            <fog:reflected rdf:resource="person-2" />
          </fog:reflection>
        </rdf:RDF>
        """;

    private const string AnonymousDeleteFog = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF
          xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
          xmlns:fog="http://fogid.net/o/"
          dbid="PA_Users_delete"
          owner="test">
          <fog:delete />
        </rdf:RDF>
        """;
}
