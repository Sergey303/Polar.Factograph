namespace Polar.Factograph.Api.Writes;

public sealed record ResourceDeleteRequest(
    string ResourceId,
    string? CassetteId = null);

public sealed record ResourceSubstituteRequest(
    string OldResourceId,
    string NewResourceId,
    string? CassetteId = null);

public sealed record ResourceDirectiveResponse(
    string Kind,
    string ResourceId,
    string? SubstituteTargetId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    bool IndexReady,
    Guid? GenerationId);
