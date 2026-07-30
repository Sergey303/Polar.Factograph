using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class CassetteDocumentPathResolverTests
{
    [Fact]
    public void Resolve_FindsOriginalAndAvailablePreviews()
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        string original = cassette.CreateFile("originals", "0001", "0042.jpg");
        string icon = cassette.CreateFile("documents", "icon", "0001", "0042.jpg");
        string small = cassette.CreateFile("documents", "small", "0001", "0042.jpg");
        string medium = cassette.CreateFile("documents", "medium", "0001", "0042.webp");
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        CassetteDocumentLocation location = resolver.Resolve(
            project,
            "iiss://testcassette@iis.nsk.su/0001/0042");

        Assert.Equal("test-cassette", location.CassetteId);
        Assert.Equal("TestCassette", location.CassetteName);
        Assert.Equal("0001", location.FolderName);
        Assert.Equal("0042", location.DocumentNumber);
        Assert.Equal(Path.GetFullPath(original), location.OriginalPath);
        Assert.Equal(Path.GetFullPath(icon), location.IconPreviewPath);
        Assert.Equal(Path.GetFullPath(small), location.SmallPreviewPath);
        Assert.Equal(Path.GetFullPath(medium), location.MediumPreviewPath);
        Assert.Null(location.NormalPreviewPath);
    }

    [Fact]
    public void Resolve_HidesPreviewOlderThanOriginal()
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        string icon = cassette.CreateFile("documents", "icon", "0001", "0042.jpg");
        string preview = cassette.CreateFile("documents", "small", "0001", "0042.jpg");
        string original = cassette.CreateFile("originals", "0001", "0042.png");
        File.SetLastWriteTimeUtc(icon, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(preview, DateTime.UtcNow.AddMinutes(-5));
        File.SetLastWriteTimeUtc(original, DateTime.UtcNow);
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        CassetteDocumentLocation location = resolver.Resolve(
            project,
            "iiss://TestCassette@iis.nsk.su/0001/0042");

        Assert.Equal(Path.GetFullPath(original), location.OriginalPath);
        Assert.Null(location.IconPreviewPath);
        Assert.Null(location.SmallPreviewPath);
    }

    [Fact]
    public void Resolve_KeepsPreviewWhenLegacyOriginalIsMissing()
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        string icon = cassette.CreateFile("documents", "icon", "0001", "0042.jpg");
        string preview = cassette.CreateFile("documents", "small", "0001", "0042.jpg");
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        CassetteDocumentLocation location = resolver.Resolve(
            project,
            "iiss://TestCassette@iis.nsk.su/0001/0042");

        Assert.Null(location.OriginalPath);
        Assert.Equal(Path.GetFullPath(icon), location.IconPreviewPath);
        Assert.Equal(Path.GetFullPath(preview), location.SmallPreviewPath);
    }

    [Fact]
    public void Resolve_RejectsAmbiguousOriginalFiles()
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        cassette.CreateFile("originals", "0001", "0042.jpg");
        cassette.CreateFile("originals", "0001", "0042.png");
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() =>
            resolver.Resolve(project, "iiss://TestCassette@iis.nsk.su/0001/0042"));

        Assert.Contains("multiple original files", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://TestCassette@iis.nsk.su/0001/0042")]
    [InlineData("iiss://TestCassette@iis.nsk.su/001/0042")]
    [InlineData("iiss://TestCassette@iis.nsk.su/0001/042")]
    public void Resolve_RejectsMalformedDocumentUris(string documentUri)
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        Assert.Throws<InvalidDataException>(() => resolver.Resolve(project, documentUri));
    }

    [Fact]
    public void Resolve_RejectsCassetteOutsideProject()
    {
        using TemporaryCassette cassette = TemporaryCassette.Create();
        ProjectDefinition project = CreateProject(cassette.Path);
        CassetteDocumentPathResolver resolver = new();

        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() =>
            resolver.Resolve(project, "iiss://OtherCassette@iis.nsk.su/0001/0042"));

        Assert.Contains("not enabled", exception.Message, StringComparison.Ordinal);
    }

    private static ProjectDefinition CreateProject(string cassettePath) => new()
    {
        ProjectId = "test-project",
        Name = "Test project",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = "index" },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "test-cassette",
                Name = "TestCassette",
                Path = cassettePath,
                Enabled = true,
                AllowWrite = false
            }
        ]
    };

    private sealed class TemporaryCassette : IDisposable
    {
        private TemporaryCassette(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryCassette Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "polar-factograph-document-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryCassette(path);
        }

        public string CreateFile(params string[] relativeParts)
        {
            string path = relativeParts.Aggregate(Path, System.IO.Path.Combine);
            string? directory = System.IO.Path.GetDirectoryName(path);
            Assert.NotNull(directory);
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "test");
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
