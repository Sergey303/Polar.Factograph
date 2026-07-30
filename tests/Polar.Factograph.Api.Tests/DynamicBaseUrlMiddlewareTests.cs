using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class DynamicBaseUrlMiddlewareTests
{
    [Theory]
    [InlineData("", "/")]
    [InlineData("/factograph", "/factograph/")]
    public async Task InvokeAsync_InsertsBaseAndApplicationMarker(
        string pathBase,
        string expectedHref)
    {
        DefaultHttpContext context = CreateContext(pathBase);
        DynamicBaseUrlMiddleware middleware = CreateMiddleware(
            "<!doctype html><html><head><title>Factograph</title></head><body></body></html>");

        await InvokeAsync(middleware, context);

        string html = await ReadResponseAsync(context);
        Assert.Contains($"<base href=\"{expectedHref}\">", html, StringComparison.Ordinal);
        Assert.Contains(
            $"<meta name=\"factograph-app-base\" content=\"{expectedHref}\">",
            html,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_AddsApplicationMarkerWhenHtmlAlreadyHasBase()
    {
        DefaultHttpContext context = CreateContext("/factograph");
        DynamicBaseUrlMiddleware middleware = CreateMiddleware(
            "<!doctype html><html><head><base href=\"./\"><title>Factograph</title></head><body></body></html>");

        await InvokeAsync(middleware, context);

        string html = await ReadResponseAsync(context);
        Assert.Contains("<base href=\"./\">", html, StringComparison.Ordinal);
        Assert.Contains(
            "<meta name=\"factograph-app-base\" content=\"/factograph/\">",
            html,
            StringComparison.Ordinal);
    }

    [Fact]
    public void InsertResourceMetadata_ReplacesTitleAndEscapesValues()
    {
        const string source =
            "<!doctype html><html><head><title>Polar.Factograph</title></head><body></body></html>";
        ResourceHtmlMetadata metadata = new(
            "Alpha <Beta>",
            "Description & check",
            "Archive \"RAS\"",
            "https://example.org/factograph/resource/iiss%3A%2F%2F1");

        string html = DynamicBaseUrlMiddleware.InsertResourceMetadata(source, metadata);

        Assert.Contains(
            "<title>Alpha &lt;Beta&gt; — Archive &quot;RAS&quot;</title>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<meta name=\"description\" content=\"Description &amp; check\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<meta property=\"og:title\" content=\"Alpha &lt;Beta&gt;\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<link rel=\"canonical\" href=\"https://example.org/factograph/resource/iiss%3A%2F%2F1\">",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("<title>Polar.Factograph</title>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void DisableStaticFileCaching_RemovesValidatorsAndPreventsStorage()
    {
        DefaultHttpContext context = new();
        context.Response.Headers.ETag = "\"static-index\"";
        context.Response.Headers.LastModified = "Wed, 29 Jul 2026 00:00:00 GMT";

        DynamicBaseUrlMiddleware.DisableStaticFileCaching(context.Response);

        Assert.False(context.Response.Headers.ContainsKey("ETag"));
        Assert.False(context.Response.Headers.ContainsKey("Last-Modified"));
        Assert.Equal("private, no-store", context.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public void MetadataText_UsesNameAndTruncatesDescription()
    {
        string longDescription = new('x', 260);
        PresentedSemanticResourcePage page = Page(
            new PresentedResourceLiteralField(
                "http://fogid.net/o/alias",
                "Псевдоним",
                "Александр Марчук",
                "Александр Марчук",
                "ru",
                null),
            new PresentedResourceLiteralField(
                "http://fogid.net/o/description",
                "Описание",
                longDescription,
                longDescription,
                "ru",
                null));

        string title = ResourceHtmlMetadataProvider.TitleOf(page);
        string description = ResourceHtmlMetadataProvider.DescriptionOf(page);

        Assert.Equal("Александр Марчук", title);
        Assert.True(description.Length <= 240);
        Assert.EndsWith("…", description, StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetPublicResourceId_UsesRawEncodedPathAndPathBase()
    {
        DefaultHttpContext context = new();
        context.Request.PathBase = "/factograph";
        context.Request.Path = "/resource/iiss://soran/1";
        context.Features.Get<IHttpRequestFeature>()!.RawTarget =
            "/factograph/resource/iiss%3A%2F%2Fsoran%2F1?source=legacy";

        string? resourceId = ResourceHtmlMetadataProvider.TryGetPublicResourceId(context.Request);

        Assert.Equal("iiss://soran/1", resourceId);
    }

    [Theory]
    [InlineData("/resource/iiss%3A%2F%2Fsoran%2F1/edit")]
    [InlineData("/resource/iiss%3A%2F%2Fsoran%2F1/relations")]
    [InlineData("/search?q=test")]
    public void TryGetPublicResourceId_IgnoresNonPublicViewRoutes(string rawTarget)
    {
        DefaultHttpContext context = new();
        context.Features.Get<IHttpRequestFeature>()!.RawTarget = rawTarget;

        Assert.Null(ResourceHtmlMetadataProvider.TryGetPublicResourceId(context.Request));
    }

    [Fact]
    public void CanonicalUrl_UsesCanonicalIdAndApplicationPathBase()
    {
        DefaultHttpContext context = new();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("archive.example", 8443);
        context.Request.PathBase = "/factograph";

        string url = ResourceHtmlMetadataProvider.CanonicalUrl(
            context.Request,
            "iiss://soran/0001/0042");

        Assert.Equal(
            "https://archive.example:8443/factograph/resource/iiss%3A%2F%2Fsoran%2F0001%2F0042",
            url);
    }

    private static PresentedSemanticResourcePage Page(
        params PresentedResourceLiteralField[] literals) => new(
        "resource-1",
        new PresentedProjectResourcePortrait(
            "resource-1",
            "person",
            "Персона",
            literals,
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

    private static DefaultHttpContext CreateContext(string pathBase)
    {
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";
        context.Request.PathBase = pathBase;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static DynamicBaseUrlMiddleware CreateMiddleware(string html) =>
        new(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html);
        });

    private static Task InvokeAsync(
        DynamicBaseUrlMiddleware middleware,
        DefaultHttpContext context) => middleware.InvokeAsync(
        context,
        new ResourceHtmlMetadataProvider(null!),
        NullLogger<DynamicBaseUrlMiddleware>.Instance);

    private static async Task<string> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
