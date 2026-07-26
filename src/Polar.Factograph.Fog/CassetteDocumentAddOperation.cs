using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentAddOperation
{
    public static async Task<CassetteDocumentWriteResult> ExecuteAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        CassetteDocumentWriterRules.RequireWritable(cassette);
        string extension = CassetteDocumentFileName.Extension(fileName);
        await using CassetteDocumentWriteLease lease =
            CassetteDocumentWriteLease.Acquire(cassette.Path);
        CassetteDocumentSlot slot = CassetteDocumentSlotAllocator.Allocate(
            cassette,
            extension);
        CassetteDocumentCopyResult copy = await CassetteDocumentAtomicFileWriter.WriteAsync(
            content,
            slot.Path,
            maxBytes,
            overwrite: false,
            cancellationToken);
        string documentUri = CassetteDocumentUri.Build(
            cassette,
            slot.FolderName,
            slot.DocumentNumber);
        return CassetteDocumentWriteResultFactory.Create(
            cassette,
            documentUri,
            slot.FolderName,
            slot.DocumentNumber,
            slot.Path,
            copy,
            replaced: false);
    }
}
