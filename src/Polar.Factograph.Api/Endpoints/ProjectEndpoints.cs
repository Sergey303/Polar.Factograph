using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/project", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        _ = ProjectAuthorization.RequireRead(context.Access);
        return Results.Ok(ProjectOverviewPresentation.Present(
            context.Project,
            context.Access));
    }
}
