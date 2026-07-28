namespace Polar.Factograph.Application;

public enum OntologyTermKind
{
    Class = 1,
    DatatypeProperty = 2,
    ObjectProperty = 3,
    EnumerationType = 4
}

public sealed record OntologyLocalizedText(
    string Value,
    string? Language);

public sealed record OntologyEnumerationState(
    string Value,
    string Label,
    string? Language);

public sealed record OntologyTerm(
    string Id,
    OntologyTermKind Kind,
    IReadOnlyList<OntologyLocalizedText> Labels,
    IReadOnlyList<OntologyLocalizedText> InverseLabels,
    string? Priority,
    string? ParentClassId,
    IReadOnlyList<string> Domains,
    IReadOnlyList<string> Ranges,
    IReadOnlyList<OntologyEnumerationState> EnumerationStates)
{
    public bool IsAbstract { get; init; }
}
