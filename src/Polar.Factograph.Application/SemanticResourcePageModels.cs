namespace Polar.Factograph.Application;

public sealed record SemanticResourceLink(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string RelationLabel);

public sealed record SemanticPhotoCard(
    string ResourceId,
    string DisplayName,
    string? DocumentUri,
    string? ContextResourceId,
    string? ContextLabel);

public sealed record PresentedSemanticResourcePage(
    string RequestedResourceId,
    PresentedProjectResourcePortrait Portrait,
    IReadOnlyList<SemanticPhotoCard> Photos,
    IReadOnlyList<SemanticResourceLink> Participants,
    IReadOnlyList<SemanticResourceLink> Organizations,
    IReadOnlyList<SemanticResourceLink> Collections,
    IReadOnlyList<SemanticResourceLink> RelatedResources);
