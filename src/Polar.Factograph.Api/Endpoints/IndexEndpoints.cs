using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Endpoints;

public static class IndexEndpoints
{
    public static IEndpointRouteBuilder MapIndexEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/admin/index/rebuild", RebuildAsync);
        return endpoints;
    }

    private static async Task<IResult> RebuildAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectIndexCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectAuthorization.RequireProjectRight(
            context.Access,
            ProjectRights.RebuildIndex);

        ProjectIndexRebuildResult result = await coordinator.RebuildAsync(
            context.Project,
            cancellationToken);
        return Results.Ok(result);
    }
}
