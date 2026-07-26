using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Endpoints;

public static class PreviewQueueEndpoints
{
    public static IEndpointRouteBuilder MapPreviewQueueEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/admin/previews/status", GetStatusAsync);
        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CassettePreviewQueueStatusReader statusReader,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectAuthorization.RequireProjectRight(
            context.Access,
            ProjectRights.RebuildIndex);
        return Results.Ok(statusReader.Read(context.Project));
    }
}