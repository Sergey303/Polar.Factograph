namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectIndexRefreshOutcome(
    bool IndexReady,
    Guid? GenerationId);
