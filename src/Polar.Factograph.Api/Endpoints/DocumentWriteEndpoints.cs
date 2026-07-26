using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Endpoints;

public static class DocumentWriteEndpoints
{
    public static IEndpointRouteBuilder MapDocumentWriteEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/documents/files", AddAsync);
        endpoints.MapPut("/api/documents/files", ReplaceAsync);
        return endpoints;
    }

    private static async Task<IResult> AddAsync(
        string fileName,
        string? cassetteId,
        HttpRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectDocumentAddCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        DocumentBinaryWriteResponse response = await coordinator.AddAsync(
            context,
            request.Body,
            fileName,
            cassetteId,
            request.ContentLength,
            cancellationToken);
        return DocumentBinaryWriteResults.Created(response);
    }

    private static async Task<IResult> ReplaceAsync(
        string uri,
        string fileName,
        HttpRequest request,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        ProjectDocumentReplaceCoordinator coordinator,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        try
        {
            DocumentBinaryWriteResponse response = await coordinator.ReplaceAsync(
                context,
                uri,
                request.Body,
                fileName,
                request.ContentLength,
                cancellationToken);
            return DocumentBinaryWriteResults.Replaced(response);
        }
        catch (KeyNotFoundException)
        {
            return DocumentBinaryWriteResults.NotFound(uri);
        }
    }
}
