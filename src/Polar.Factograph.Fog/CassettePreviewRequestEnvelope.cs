namespace Polar.Factograph.Fog;

internal sealed record CassettePreviewRequestEnvelope(
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
    bool Replaced);
