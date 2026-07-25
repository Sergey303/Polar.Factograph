namespace Polar.Factograph.Application;

public sealed record ProjectCollectionItem(
    string MembershipResourceId,
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string MembershipCassetteId,
    string ResourceCassetteId);

public sealed record ProjectCollectionContents(
    string CollectionId,
    IReadOnlyList<ProjectCollectionItem> Items);

internal sealed record ProjectCollectionItemReference(
    string MembershipResourceId,
    string ResourceId,
    string MembershipCassetteId);
