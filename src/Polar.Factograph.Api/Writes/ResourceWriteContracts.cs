namespace Polar.Factograph.Api.Writes;

public sealed record ResourceWriteRequest(
    string TypeId,
    IReadOnlyList<ResourceWritePropertyRequest> Properties,
    string? ResourceId = null,
    string? CassetteId = null);

public sealed record ResourceWritePropertyRequest(
    string Predicate,
    string Value,
    string Kind = "literal",
    string? Language = null,
    string? DataType = null);

public sealed record ResourceWriteResponse(
    string ResourceId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    bool IndexReady,
    Guid? GenerationId);
