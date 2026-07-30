using Polar.Factograph.Api.Collections;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Endpoints;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/collections/items", GetItemsAsync);
        return endpoints;
    }

    private static async Task<IResult> GetItemsAsync(
        string id,
        int limit,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        ProjectCollectionContents? contents = await context.Collections.GetAsync(
            id,
            context.Access,
            limit == 0 ? 100 : limit,
            string.IsNullOrWhiteSpace(lang) ? "ru" : lang,
            cancellationToken);

        return contents is null
            ? Results.NotFound(new ApiError(
                "collection_not_found",
                $"Collection was not found: {id}"))
            : Results.Ok(CollectionContentsPresentation.Present(contents, context.Access));
    }
}
