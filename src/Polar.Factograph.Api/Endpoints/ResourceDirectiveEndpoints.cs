using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Endpoints;

public static class ResourceDirectiveEndpoints
{
    public static IEndpointRouteBuilder MapResourceDirectiveEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/resources/delete", DeleteAsync);
        endpoints.MapPost("/api/resources/substitute", SubstituteAsync);
        return endpoints;
    }

    private static async Task<IResult> DeleteAsync(
        ResourceDeleteRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectDirectiveWriteCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectDirectiveWriteOutcome outcome = await coordinator.DeleteAsync(
            context,
            ResourceDirectiveRequestMapper.Map(request),
            request.CassetteId,
            cancellationToken);
        return ToResult(outcome);
    }

    private static async Task<IResult> SubstituteAsync(
        ResourceSubstituteRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectDirectiveWriteCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectDirectiveWriteOutcome outcome = await coordinator.SubstituteAsync(
            context,
            ResourceDirectiveRequestMapper.Map(request),
            request.CassetteId,
            cancellationToken);
        return ToResult(outcome);
    }

    private static IResult ToResult(ProjectDirectiveWriteOutcome outcome)
    {
        ResourceDirectiveResponse response = new(
            outcome.Kind,
            outcome.ResourceId,
            outcome.SubstituteTargetId,
            outcome.CassetteId,
            outcome.ModifiedAtUtc,
            outcome.IndexReady,
            outcome.GenerationId);
        string location = $"/api/resources/portrait?id={Uri.EscapeDataString(outcome.ResourceId)}";
        return outcome.IndexReady
            ? Results.Ok(response)
            : Results.Accepted(location, response);
    }
}
