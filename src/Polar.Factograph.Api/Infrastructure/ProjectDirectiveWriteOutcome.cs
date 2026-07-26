namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectDirectiveWriteOutcome(
    string Kind,
    string ResourceId,
    string? SubstituteTargetId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    bool IndexReady,
    Guid? GenerationId);
