using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Endpoints;

public static class ResourceWriteEndpoints
{
    public static IEndpointRouteBuilder MapResourceWriteEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/resources", WriteAsync);
        return endpoints;
    }

    private static async Task<IResult> WriteAsync(
        ResourceWriteRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectResourceWriteCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        FogResourceWriteRequest fogRequest = ResourceWriteRequestMapper.Map(request);
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectResourceWriteOutcome outcome = await coordinator.WriteAsync(
            context,
            fogRequest,
            request.CassetteId,
            cancellationToken);
        ResourceWriteResponse response = new(
            outcome.ResourceId,
            outcome.CassetteId,
            outcome.ModifiedAtUtc,
            outcome.IndexReady,
            outcome.GenerationId);
        string location = $"/api/resources/portrait?id={Uri.EscapeDataString(outcome.ResourceId)}";

        return outcome.IndexReady
            ? Results.Created(location, response)
            : Results.Accepted(location, response);
    }
}
