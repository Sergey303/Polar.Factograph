using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed record CassetteDocumentWriteResult(
    string CassetteId,
    string CassetteName,
    string DocumentUri,
    string FolderName,
    string DocumentNumber,
    string FileName,
    long Length,
    string Sha256,
    bool Replaced);

public interface ICassetteDocumentWriter
{
    Task<CassetteDocumentWriteResult> AddAsync(
        CassetteDefinition cassette,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default);

    Task<CassetteDocumentWriteResult> ReplaceAsync(
        CassetteDefinition cassette,
        CassetteDocumentLocation location,
        Stream content,
        string fileName,
        long maxBytes,
        CancellationToken cancellationToken = default);
}
