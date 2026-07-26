using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Writing;

public sealed record ResourceWriteBody(
    string? TypeId,
    IReadOnlyList<ResourcePropertyWriteBody>? Properties,
    string? CassetteId = null);

public sealed record ResourcePropertyWriteBody(
    string? Predicate,
    string? Kind,
    string? Value,
    string? Language = null,
    string? DataType = null);

public sealed record ProjectResourceWriteCommand(
    FogResourceWriteRequest Resource,
    string? CassetteId);

public sealed record ProjectResourceWriteResult(
    string ResourceId,
    string CassetteId,
    DateTime ModifiedAtUtc,
    Guid GenerationId,
    int SourceFiles,
    ProjectIndexBuildStatistics Statistics);
