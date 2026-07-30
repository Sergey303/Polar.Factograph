using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/documents");
        group.MapGet("/location", GetLocationAsync);
        group.MapGet("/content", GetContentAsync);
        group.MapGet("/image", GetImageAsync);
        return endpoints;
    }

    private static async Task<IResult> GetLocationAsync(
        string uri,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CassetteDocumentPathResolver resolver,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        CassetteDocumentLocation? location = ResolveAuthorized(context, resolver, uri);
        if (location is null)
        {
            return NotFound(uri);
        }

        return Results.Ok(DocumentLocationPresentation.Present(
            location,
            context.Access));
    }

    private static async Task<IResult> GetContentAsync(
        string uri,
        string variant,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CassetteDocumentPathResolver resolver,
        DocumentContentTypeResolver contentTypes,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        CassetteDocumentLocation? location = ResolveAuthorized(context, resolver, uri);
        if (location is null)
        {
            return NotFound(uri);
        }

        string? path = DocumentVariantSelector.Select(
            location,
            DocumentVariantSelector.Parse(variant));
        if (path is null || !File.Exists(path))
        {
            return Results.NotFound(new ApiError(
                "document_variant_not_found",
                $"Document variant was not found: {variant}"));
        }

        return Results.File(
            path,
            contentTypes.Resolve(path),
            enableRangeProcessing: true);
    }

    private static async Task<IResult> GetImageAsync(
        string uri,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CassetteDocumentPathResolver resolver,
        DocumentContentTypeResolver contentTypes,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        CassetteDocumentLocation? location = ResolveAuthorized(context, resolver, uri);
        if (location is null)
        {
            return NotFound(uri);
        }

        DocumentImageSelection? image = DocumentImageSelector.Select(location, contentTypes);
        if (image is null || !File.Exists(image.Path))
        {
            return Results.NotFound(new ApiError(
                "document_image_not_found",
                $"Document image was not found: {uri}"));
        }

        return Results.File(
            image.Path,
            contentTypes.Resolve(image.Path),
            enableRangeProcessing: true);
    }

    private static CassetteDocumentLocation? ResolveAuthorized(
        ProjectAccessContext context,
        CassetteDocumentPathResolver resolver,
        string uri)
    {
        IReadOnlySet<string> readable = ProjectAuthorization.RequireRead(context.Access);
        try
        {
            CassetteDocumentLocation location = resolver.Resolve(context.Project, uri);
            return readable.Contains(location.CassetteId) ? location : null;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    private static IResult NotFound(string uri) => Results.NotFound(new ApiError(
        "document_not_found",
        $"Document was not found: {uri}"));
}
