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
    string? SortDate = null);

public sealed record SemanticPhotoCard(
    string ResourceId,
    string DisplayName,
    string? DocumentUri,
    string? ContextResourceId,
    string? ContextLabel,
    string? DisplayDate = null,
    string? SortDate = null);

public sealed record PresentedSemanticResourcePage(
    string RequestedResourceId,
    PresentedProjectResourcePortrait Portrait,
    IReadOnlyList<SemanticPhotoCard> Photos,
    IReadOnlyList<SemanticResourceLink> Participants,
    IReadOnlyList<SemanticResourceLink> Organizations,
    IReadOnlyList<SemanticResourceLink> Collections,
    IReadOnlyList<SemanticResourceLink> RelatedResources);
