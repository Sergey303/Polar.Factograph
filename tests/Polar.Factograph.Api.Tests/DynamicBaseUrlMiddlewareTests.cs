using Microsoft.AspNetCore.Http;
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

        await middleware.InvokeAsync(context);

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

        await middleware.InvokeAsync(context);

        string html = await ReadResponseAsync(context);
        Assert.Contains("<base href=\"./\">", html, StringComparison.Ordinal);
        Assert.Contains(
            "<meta name=\"factograph-app-base\" content=\"/factograph/\">",
            html,
            StringComparison.Ordinal);
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

    private static async Task<string> ReadResponseAsync(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        using StreamReader reader = new(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
