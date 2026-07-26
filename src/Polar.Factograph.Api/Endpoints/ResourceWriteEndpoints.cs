using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writing;

namespace Polar.Factograph.Api.Endpoints;

public static class ResourceWriteEndpoints
{
    public static IEndpointRouteBuilder MapResourceWriteEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/resources", CreateAsync);
        endpoints.MapPut("/api/resources/{resourceId}", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        ResourceWriteBody body,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ResourceWriteRequestMapper mapper,
        ProjectResourceWriteCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectResourceWriteResult result = await WriteAsync(
            body,
            resourceId: null,
            httpContext,
            contextFactory,
            mapper,
            coordinator,
            cancellationToken);
        string location = "/api/resources/portrait?id=" +
                          Uri.EscapeDataString(result.ResourceId);
        return Results.Created(location, result);
    }

    private static async Task<IResult> UpdateAsync(
        string resourceId,
        ResourceWriteBody body,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ResourceWriteRequestMapper mapper,
        ProjectResourceWriteCoordinator coordinator,
        CancellationToken cancellationToken) => Results.Ok(await WriteAsync(
        body,
        resourceId,
        httpContext,
        contextFactory,
        mapper,
        coordinator,
        cancellationToken));

    private static async Task<ProjectResourceWriteResult> WriteAsync(
        ResourceWriteBody body,
        string? resourceId,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ResourceWriteRequestMapper mapper,
        ProjectResourceWriteCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectResourceWriteCommand command = mapper.Map(body, resourceId);
        return await coordinator.WriteAsync(
            context.Project,
            context.Access,
            command,
            cancellationToken);
    }
}
