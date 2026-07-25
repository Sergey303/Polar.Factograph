using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/search");
        group.MapGet("/names", SearchNamesAsync);
        group.MapGet("/words", SearchWordsAsync);
        return endpoints;
    }

    private static async Task<IResult> SearchNamesAsync(
        string q,
        int? limit,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        IReadOnlyList<ProjectResourceSearchResult> results =
            await context.Reads.SearchByNameAsync(
                q,
                context.Access,
                NormalizeLimit(limit),
                NormalizeLanguage(lang),
                cancellationToken);
        return Results.Ok(results);
    }

    private static async Task<IResult> SearchWordsAsync(
        string q,
        int? limit,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        IReadOnlyList<ProjectResourceSearchResult> results =
            await context.Reads.SearchByWordsAsync(
                q,
                context.Access,
                NormalizeLimit(limit),
                NormalizeLanguage(lang),
                cancellationToken);
        return Results.Ok(results);
    }

    private static int NormalizeLimit(int? limit) => limit ?? 50;

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "ru" : language;
}
