namespace Polar.Factograph.Api.Writes;

public sealed record OntologyWriteOptionResponse(
    string Value,
    string Label);

public sealed record OntologyWritePropertyResponse(
    string Id,
    string Label,
    string Kind,
    IReadOnlyList<string> Ranges,
    IReadOnlyList<OntologyWriteOptionResponse> Options);

public sealed record OntologyWriteClassResponse(
    string Id,
    string Label,
    string? ParentClassId,
    bool IsAbstract,
    IReadOnlyList<OntologyWritePropertyResponse> Properties);

public sealed record OntologyWriteSchemaResponse(
    IReadOnlyList<OntologyWriteClassResponse> Classes);
