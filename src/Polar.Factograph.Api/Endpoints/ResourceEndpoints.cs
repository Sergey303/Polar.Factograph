using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Endpoints;

public static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/resources/portrait", GetPortraitAsync);
        endpoints.MapGet("/api/resources/page", GetPageAsync);
        return endpoints;
    }

    private static async Task<IResult> GetPortraitAsync(
        string id,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        _ = ProjectAuthorization.RequireWritableCassetteRight(
            context.Access,
            CassetteRights.WriteMetadata);
        PresentedProjectResourcePortrait? portrait = await context.Portraits.GetAsync(
            id,
            context.Access,
            NormalizeLanguage(lang),
            cancellationToken);

        return portrait is null
            ? NotFound(id)
            : Results.Ok(portrait);
    }

    private static async Task<IResult> GetPageAsync(
        string id,
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectReadContext context = await contextFactory.CreateReadAsync(
            httpContext,
            cancellationToken);
        PresentedSemanticResourcePage? page = await context.SemanticPages.GetCompactAsync(
            id,
            context.Access,
            NormalizeLanguage(lang),
            cancellationToken);

        return page is null
            ? NotFound(id)
            : Results.Ok(page);
    }

    private static string NormalizeLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? "ru" : language;

    private static IResult NotFound(string id) =>
        Results.NotFound(new ApiError("resource_not_found", $"Resource was not found: {id}"));
}
