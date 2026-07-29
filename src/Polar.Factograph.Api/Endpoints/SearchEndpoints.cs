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
        group.MapGet("/duplicates", SearchDuplicatesAsync);
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

    private static async Task<IResult> SearchDuplicatesAsync(
        string type,
        string predicate,
        string value,
        int? limit,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        IReadOnlyList<PotentialDuplicateResource> results =
            await context.PotentialDuplicates.FindAsync(
                type,
                predicate,
                value,
                context.Access,
                Math.Min(NormalizeLimit(limit), 50),
                NormalizeLanguage(lang),
                cancellationToken);
        return Results.Ok(results);
    }

    private static int NormalizeLimit(int? limit) => limit ?? 50;

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "ru" : language;
}
