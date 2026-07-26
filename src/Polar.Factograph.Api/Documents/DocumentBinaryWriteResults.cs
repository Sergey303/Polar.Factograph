using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Documents;

internal static class DocumentBinaryWriteResults
{
    public static IResult Created(DocumentBinaryWriteResponse response) =>
        Results.Created(Location(response.DocumentUri), response);

    public static IResult Replaced(DocumentBinaryWriteResponse response) =>
        Results.Ok(response);

    public static IResult NotFound(string documentUri) => Results.NotFound(new ApiError(
        "document_not_found",
        $"Document was not found: {documentUri}"));

    private static string Location(string documentUri) =>
        $"/api/documents/location?uri={Uri.EscapeDataString(documentUri)}";
}
