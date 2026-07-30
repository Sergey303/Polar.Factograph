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
        group.MapGet("/classes", SearchClassesAsync);
        group.MapGet("/by-type", SearchByTypeAsync);
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
        return Results.Ok(results
            .Select(SearchResponsePresentation.Present)
            .ToArray());
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
        return Results.Ok(results
            .Select(SearchResponsePresentation.Present)
            .ToArray());
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

    private static async Task<IResult> SearchClassesAsync(
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
        ProjectAuthorization.RequireSearch(context.Access);
        IReadOnlyList<OntologyClassSearchSuggestion> results = context.TypeSearch.Suggest(
            q,
            Math.Min(NormalizeLimit(limit, 8), 20),
            NormalizeLanguage(lang));
        return Results.Ok(results);
    }

    private static async Task<IResult> SearchByTypeAsync(
        string type,
        int? offset,
        int? limit,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        ProjectResourceTypeSearchPage page = await context.TypeSearch.SearchAsync(
            type,
            context.Access,
            Math.Max(offset ?? 0, 0),
            Math.Min(NormalizeLimit(limit), 100),
            NormalizeLanguage(lang),
            cancellationToken);
        return Results.Ok(SearchResponsePresentation.Present(page));
    }

    private static int NormalizeLimit(int? limit, int fallback = 50) => limit ?? fallback;

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "ru" : language;
}
