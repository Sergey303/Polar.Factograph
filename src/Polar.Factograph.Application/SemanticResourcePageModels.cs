namespace Polar.Factograph.Application;

public sealed record SemanticResourceLink(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string RelationLabel,
    string? RelationResourceId = null,
    string? DocumentUri = null,
    string? DisplayDate = null,
    string? SortDate = null,
    string? GroupKey = null,
    string? GroupLabel = null,
    bool HasDocument = false);

public sealed record SemanticRelationMember(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string? RoleLabel,
    string? DocumentUri = null,
    bool HasDocument = false);

public sealed record SemanticRelationValue(
    string Label,
    string Value);

public sealed record SemanticRelationEntry(
    string Key,
    string Title,
    string? RelationResourceId,
    string? RelationType,
    string? RelationTypeLabel,
    string GroupKey,
    string GroupLabel,
    string? DisplayDate,
    string? SortDate,
    string? DocumentUri,
    IReadOnlyList<SemanticRelationMember> Members,
    IReadOnlyList<SemanticRelationValue> Values);

public sealed record SemanticPhotoCard(
    string ResourceId,
    string DisplayName,
    string? DocumentUri,
    string? ContextResourceId,
    string? ContextLabel,
    string? DisplayDate = null,
    string? SortDate = null,
    bool HasDocument = false);

public sealed record PresentedSemanticResourcePage(
    string RequestedResourceId,
    PresentedProjectResourcePortrait Portrait,
    IReadOnlyList<SemanticPhotoCard> Photos,
    IReadOnlyList<SemanticResourceLink> Participants,
    IReadOnlyList<SemanticResourceLink> Organizations,
    IReadOnlyList<SemanticResourceLink> Collections,
    IReadOnlyList<SemanticResourceLink> RelatedResources)
{
    public IReadOnlyList<SemanticResourceLink> Links { get; init; } =
        Array.Empty<SemanticResourceLink>();

    public IReadOnlyList<SemanticRelationEntry> Entries { get; init; } =
        Array.Empty<SemanticRelationEntry>();
}
