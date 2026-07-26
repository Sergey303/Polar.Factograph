using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Previews;
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
        PreviewWorkerRuntimeState runtime,
        IOptions<PreviewWorkerOptions> options,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectAuthorization.RequireProjectRight(
            context.Access,
            ProjectRights.RebuildIndex);
        PreviewWorkerRuntimeSnapshot worker = runtime.Read();
        PreviewWorkerHealth health = PreviewWorkerHealthEvaluator.Evaluate(
            worker,
            options.Value,
            DateTimeOffset.UtcNow);
        return Results.Ok(new PreviewSubsystemStatus(
            statusReader.Read(context.Project),
            worker,
            health));
    }
}
