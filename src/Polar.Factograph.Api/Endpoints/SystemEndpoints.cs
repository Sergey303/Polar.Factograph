using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Previews;

namespace Polar.Factograph.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system/health", GetHealth);
        endpoints.MapGet("/", () => Results.Redirect("/api/system/health"));
        return endpoints;
    }

    private static IResult GetHealth(
        PreviewWorkerRuntimeState runtime,
        IOptions<PreviewWorkerOptions> options)
    {
        PreviewWorkerHealth previews = PreviewWorkerHealthEvaluator.Evaluate(
            runtime.Read(),
            options.Value,
            DateTimeOffset.UtcNow);
        return Results.Ok(new
        {
            service = "Polar.Factograph.Api",
            status = previews.Degraded ? "degraded" : "ok",
            previews = new
            {
                state = previews.State,
                enabled = previews.Enabled
            }
        });
    }
}
