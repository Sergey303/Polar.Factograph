namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectFogMutationOutcome<T>(
    T Written,
    string CassetteId,
    bool IndexReady,
    Guid? GenerationId);
