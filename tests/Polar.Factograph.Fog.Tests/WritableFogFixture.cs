using System.Text;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Fog.Tests;

internal sealed class WritableFogFixture : IAsyncDisposable
{
    private WritableFogFixture(
        string directory,
        FogSourceDescriptor source)
    {
        Directory = directory;
        Source = source;
    }

    public string Directory { get; }
    public FogSourceDescriptor Source { get; }

    public static async Task<WritableFogFixture> CreateAsync()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-write-tests",
            Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "current.fog");
        await File.WriteAllTextAsync(path, Xml, new UTF8Encoding(false));
        FileInfo file = new(path);

        return new WritableFogFixture(
            directory,
            new FogSourceDescriptor(
                "cassette",
                "Cassette",
                file.FullName,
                "database",
                "iiss://Cassette@iis.nsk.su",
                "owner",
                "p",
                7,
                Writable: true,
                IsCassetteMetadata: true,
                file.Length,
                file.LastWriteTimeUtc));
    }

    public ValueTask DisposeAsync()
    {
        if (System.IO.Directory.Exists(Directory))
        {
            System.IO.Directory.Delete(Directory, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                 owner="owner" prefix="p" counter="007">
          <person rdf:about="existing" mT="2020-01-01 00:00:00Z">
            <name>Existing</name>
          </person>
        </rdf:RDF>
        """;
}
