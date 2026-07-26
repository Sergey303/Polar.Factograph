namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectResourceWriteOutcome(
    string ResourceId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    bool IndexReady,
    Guid? GenerationId);
