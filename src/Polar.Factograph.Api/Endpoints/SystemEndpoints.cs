namespace Polar.Factograph.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/system/health", () => Results.Ok(new
        {
            service = "Polar.Factograph.Api",
            status = "ok"
        }));
        endpoints.MapGet("/", () => Results.Redirect("/api/system/health"));
        return endpoints;
    }
}
