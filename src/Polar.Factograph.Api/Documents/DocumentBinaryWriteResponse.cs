namespace Polar.Factograph.Api.Documents;

public sealed record DocumentBinaryWriteResponse(
    string CassetteId,
    string CassetteName,
    string DocumentUri,
    string FolderName,
    string DocumentNumber,
    string FileName,
    long Length,
    string Sha256,
    bool Replaced,
    string PreviewState,
    string? PreviewRequestId,
    DateTimeOffset? PreviewQueuedAtUtc);
