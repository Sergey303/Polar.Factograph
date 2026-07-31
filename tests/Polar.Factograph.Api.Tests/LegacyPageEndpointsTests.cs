using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Polar.Factograph.Api.Endpoints;

namespace Polar.Factograph.Api.Tests;

public sealed class LegacyPageEndpointsTests
{
    [Fact]
    public async Task RedirectLegacyDefault_preserves_path_base_and_encodes_resource_id()
    {
        using ServiceProvider services = CreateRequestServices();
        DefaultHttpContext context = new()
        {
            RequestServices = services
        };
        context.Request.PathBase = "/factograph";
        IResult result = LegacyPageEndpoints.RedirectLegacyDefault(
            context,
            "iiss://soran/0001/0042");

        await result.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status301MovedPermanently, context.Response.StatusCode);
        Assert.Equal(
            "/factograph/resource/iiss%3A%2F%2Fsoran%2F0001%2F0042",
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task RedirectLegacyDefault_sends_missing_id_to_search_temporarily()
    {
        using ServiceProvider services = CreateRequestServices();
        DefaultHttpContext context = new()
        {
            RequestServices = services
        };
        context.Request.PathBase = "/catalog/";
        IResult result = LegacyPageEndpoints.RedirectLegacyDefault(context, "   ");

        await result.ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal("/catalog/search", context.Response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData("", "/search", "/search")]
    [InlineData("/catalog", "resource/abc", "/catalog/resource/abc")]
    [InlineData("/catalog/", "/search", "/catalog/search")]
    public void ApplicationLocation_joins_base_and_route_without_double_slashes(
        string pathBase,
        string route,
        string expected)
    {
        string result = LegacyPageEndpoints.ApplicationLocation(pathBase, route);

        Assert.Equal(expected, result);
    }

    private static ServiceProvider CreateRequestServices() =>
        new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
}
