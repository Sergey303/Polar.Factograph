using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/resources/portrait", GetPortraitAsync);
        return endpoints;
    }

    private static async Task<IResult> GetPortraitAsync(
        string id,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        ProjectResourcePortrait? portrait = await context.Reads.GetPortraitAsync(
            id,
            context.Access,
            cancellationToken);

        return portrait is null
            ? Results.NotFound(new ApiError("resource_not_found", $"Resource was not found: {id}"))
            : Results.Ok(portrait);
    }
}
