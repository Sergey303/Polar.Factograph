namespace Polar.Factograph.Api.Infrastructure;

public sealed record ProjectIndexRuntimeStatus(
    string State,
    bool Dirty,
    DateTimeOffset? DirtySinceUtc,
    string CurrentPointerState,
    Guid? CurrentGenerationId,
    bool CurrentGenerationAvailable,
    int CompletedGenerationCount,
    int BuildingGenerationCount);
