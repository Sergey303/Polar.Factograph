namespace Polar.Factograph.Fog;

public sealed record CassettePreviewRequest(
    string RequestId,
    DateTimeOffset RequestedAtUtc,
    string CassetteId,
    string CassetteName,
    string DocumentUri,
    string FolderName,
    string DocumentNumber,
    string OriginalFileName,
    long OriginalLength,
    string OriginalSha256,
    bool Replaced,
    int Attempt = 0,
    DateTimeOffset? NotBeforeUtc = null,
    string? LastError = null);