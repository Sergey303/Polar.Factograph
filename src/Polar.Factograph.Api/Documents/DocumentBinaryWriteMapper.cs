using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Documents;

internal static class DocumentBinaryWriteMapper
{
    public static DocumentBinaryWriteResponse Map(
        CassetteDocumentWriteResult result,
        CassettePreviewQueueResult preview)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(preview);
        return new DocumentBinaryWriteResponse(
            result.CassetteId,
            result.CassetteName,
            result.DocumentUri,
            result.FolderName,
            result.DocumentNumber,
            result.FileName,
            result.Length,
            result.Sha256,
            result.Replaced,
            preview.State,
            preview.RequestId,
            preview.QueuedAtUtc);
    }
}
