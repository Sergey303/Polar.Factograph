using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
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
            "Альфа <Бета>",
            "Описание & проверка",
            "Каталог \"СО РАН\"",
            "https://example.org/factograph/resource/iiss%3A%2F%2F1");

        string html = DynamicBaseUrlMiddleware.InsertResourceMetadata(source, metadata);

        Assert.Contains(
            "<title>Альфа &lt;Бета&gt; — Каталог &quot;СО РАН&quot;</title>",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<meta name=\"description\" content=\"Описание &amp; проверка\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<meta property=\"og:title\" content=\"Альфа &lt;Бета&gt;\">",
            html,
            StringComparison.Ordinal);
        Assert.Contains(
            "<link rel=\"canonical\" href=\"https://example.org/factograph/resource/iiss%3A%2F%2F1\">",
            html,
            StringComparison.Ordinal);
        Assert.DoesNotContain("<title>Polar.Factograph</title>", html, StringComparison.Ordinal);
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
