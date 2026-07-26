using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal static class CassetteDocumentReplaceOperation
{
    public static async Task<CassetteDocumentWriteResult> ExecuteAsync(
        CassetteDefinition cassette,
        CassetteDocumentLocation location,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        CassetteDocumentWriterRules.RequireWritable(cassette);
        string originalPath = CassetteDocumentWriterRules.RequireOriginal(
            cassette,
            location);
        CassetteDocumentFileName.RequireSameExtension(fileName, originalPath);
        await using CassetteDocumentWriteLease lease =
            CassetteDocumentWriteLease.Acquire(cassette.Path);
        if (!File.Exists(originalPath))
        {
            throw new KeyNotFoundException(
                $"Document original was not found: {location.DocumentUri}");
        }

        CassetteDocumentCopyResult copy = await CassetteDocumentAtomicFileWriter.WriteAsync(
            content,
            originalPath,
            maxBytes,
            overwrite: true,
            cancellationToken);
        return CassetteDocumentWriteResultFactory.Create(
            cassette,
            location.DocumentUri,
            location.FolderName,
            location.DocumentNumber,
            originalPath,
            copy,
            replaced: true);
    }
}
