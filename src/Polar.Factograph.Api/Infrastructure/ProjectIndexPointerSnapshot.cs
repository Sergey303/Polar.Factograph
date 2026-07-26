namespace Polar.Factograph.Api.Infrastructure;

internal sealed record ProjectIndexPointerSnapshot(
    string State,
    Guid? GenerationId,
    bool GenerationAvailable);
