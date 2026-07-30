using Microsoft.AspNetCore.Http;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ResourceHtmlMetadataImageTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        "polar-factograph-og-image-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ImageUrl_UsesLargestAuthorizedImagePreview()
    {
        string normal = CreateFile("documents", "normal", "0001", "0042.jpg");
        CreateFile("documents", "small", "0001", "0042.jpg");
        ProjectDefinition project = Project();
        ProjectAccessSnapshot access = Access(canRead: true);
        DefaultHttpContext context = Context();

        string? imageUrl = ResourceHtmlMetadataProvider.ImageUrl(
            context.Request,
            Page("iiss://TestCassette@iis.nsk.su/0001/0042"),
            project,
            access,
            new CassetteDocumentPathResolver(),
            new DocumentContentTypeResolver());

        Assert.True(File.Exists(normal));
        Assert.Equal(
            "https://archive.example/factograph/api/documents/content" +
            "?uri=iiss%3A%2F%2FTestCassette%40iis.nsk.su%2F0001%2F0042&variant=normal",
            imageUrl);
    }

    [Fact]
    public void ImageUrl_RejectsPreviewFromUnreadableCassette()
    {
        CreateFile("documents", "normal", "0001", "0042.jpg");

        string? imageUrl = ResourceHtmlMetadataProvider.ImageUrl(
            Context().Request,
            Page("iiss://TestCassette@iis.nsk.su/0001/0042"),
            Project(),
            Access(canRead: false),
            new CassetteDocumentPathResolver(),
            new DocumentContentTypeResolver());

        Assert.Null(imageUrl);
    }

    [Fact]
    public void ImageUrl_DoesNotPublishNonImageOriginalWithoutPreview()
    {
        CreateFile("originals", "0001", "0042.pdf");

        string? imageUrl = ResourceHtmlMetadataProvider.ImageUrl(
            Context().Request,
            Page("iiss://TestCassette@iis.nsk.su/0001/0042"),
            Project(),
            Access(canRead: true),
            new CassetteDocumentPathResolver(),
            new DocumentContentTypeResolver());

        Assert.Null(imageUrl);
    }

    [Fact]
    public void InsertResourceMetadata_AddsImageCardsOnlyWhenImageExists()
    {
        const string source =
            "<!doctype html><html><head><title>Factograph</title></head><body></body></html>";
        ResourceHtmlMetadata metadata = new(
            "Archive item",
            "Description",
            "Archive",
            "https://archive.example/resource/1",
            "https://archive.example/api/documents/content?uri=1&amp;variant=normal");

        string withImage = DynamicBaseUrlMiddleware.InsertResourceMetadata(source, metadata);
        string withoutImage = DynamicBaseUrlMiddleware.InsertResourceMetadata(
            source,
            metadata with { ImageUrl = null });

        Assert.Contains("property=\"og:image\"", withImage, StringComparison.Ordinal);
        Assert.Contains("name=\"twitter:image\"", withImage, StringComparison.Ordinal);
        Assert.Contains("summary_large_image", withImage, StringComparison.Ordinal);
        Assert.DoesNotContain("property=\"og:image\"", withoutImage, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"twitter:image\"", withoutImage, StringComparison.Ordinal);
        Assert.Contains("content=\"summary\"", withoutImage, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateFile(params string[] relativeParts)
    {
        string path = relativeParts.Aggregate(_root, Path.Combine);
        string? directory = Path.GetDirectoryName(path);
        Assert.NotNull(directory);
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "test");
        return path;
    }

    private ProjectDefinition Project() => new()
    {
        ProjectId = "test",
        Name = "Archive",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = "index" },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "cassette",
                Name = "TestCassette",
                Path = _root,
                Enabled = true,
                AllowWrite = false
            }
        ]
    };

    private static ProjectAccessSnapshot Access(bool canRead) => new(
        "viewer",
        IsMember: true,
        new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cassette"] = new CassetteAccessSnapshot(
                "cassette",
                Enabled: true,
                AllowWrite: false,
                canRead
                    ? new HashSet<string>([CassetteRights.Read], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal))
        },
        DefaultWriteCassetteId: null);

    private static DefaultHttpContext Context()
    {
        DefaultHttpContext context = new();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("archive.example");
        context.Request.PathBase = "/factograph";
        return context;
    }

    private static PresentedSemanticResourcePage Page(string documentUri) => new(
        "resource-1",
        new PresentedProjectResourcePortrait(
            "resource-1",
            "document",
            "Документ",
            [
                new PresentedResourceLiteralField(
                    "http://fogid.net/o/uri",
                    "URI",
                    documentUri,
                    documentUri,
                    null,
                    null)
            ],
            Array.Empty<PresentedResourceDirectLink>(),
            Array.Empty<PresentedResourceInverseLink>(),
            new ResourceProvenance(
                Guid.NewGuid(),
                "cassette",
                "source.fog",
                DateTimeOffset.UnixEpoch)),
        Array.Empty<SemanticPhotoCard>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>(),
        Array.Empty<SemanticResourceLink>());
}
