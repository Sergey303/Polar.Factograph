using Polar.Factograph.Api.Collections;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Endpoints;

public static class CollectionMutationEndpoints
{
    public static IEndpointRouteBuilder MapCollectionMutationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/collections/items", AddAsync);
        endpoints.MapPost("/api/collections/items/remove", RemoveAsync);
        return endpoints;
    }

    private static async Task<IResult> AddAsync(
        CollectionItemAddRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectCollectionAddCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        CollectionItemAddRequest normalized =
            CollectionMutationRequestMapper.Normalize(request);
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        CollectionItemMutationResponse response = await coordinator.AddAsync(
            context,
            normalized,
            cancellationToken);
        return CollectionMutationResults.Added(response);
    }

    private static async Task<IResult> RemoveAsync(
        CollectionItemRemoveRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectCollectionRemoveCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        CollectionItemRemoveRequest normalized =
            CollectionMutationRequestMapper.Normalize(request);
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        CollectionItemMutationResponse response = await coordinator.RemoveAsync(
            context,
            normalized,
            cancellationToken);
        return CollectionMutationResults.Removed(response);
    }
}
