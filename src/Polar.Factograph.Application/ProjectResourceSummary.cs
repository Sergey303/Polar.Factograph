namespace Polar.Factograph.Application;

internal sealed record ProjectResourceSummary(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string SourceCassetteId);
