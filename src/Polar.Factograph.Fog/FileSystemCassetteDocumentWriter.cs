using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed class FileSystemCassetteDocumentWriter : ICassetteDocumentWriter
{
    public Task<CassetteDocumentWriteResult> AddAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default) =>
        CassetteDocumentAddOperation.ExecuteAsync(
            cassette,
            content,
            fileName,
            maxBytes,
            cancellationToken);

    public Task<CassetteDocumentWriteResult> ReplaceAsync(
        CassetteDefinition cassette,
        CassetteDocumentLocation location,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default) =>
        CassetteDocumentReplaceOperation.ExecuteAsync(
            cassette,
            location,
            content,
            fileName,
            maxBytes,
            cancellationToken);
}
