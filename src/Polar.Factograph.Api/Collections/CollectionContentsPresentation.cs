using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Collections;

public sealed record CollectionItemResponse(
    string ResourceId,
    string DisplayName,
    string? Type,
    string? TypeLabel,
    string? MembershipResourceId,
    string? MembershipCassetteId);

public sealed record CollectionContentsResponse(
    string CollectionId,
    IReadOnlyList<CollectionItemResponse> Items);

public static class CollectionContentsPresentation
{
    public static CollectionContentsResponse Present(
        ProjectCollectionContents contents,
        ProjectAccessSnapshot access)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentNullException.ThrowIfNull(access);

        return new CollectionContentsResponse(
            contents.CollectionId,
            contents.Items.Select(item => PresentItem(item, access)).ToArray());
    }

    private static CollectionItemResponse PresentItem(
        ProjectCollectionItem item,
        ProjectAccessSnapshot access)
    {
        bool canDeleteMembership = CanDeleteMembership(item.MembershipCassetteId, access);
        return new CollectionItemResponse(
            item.ResourceId,
            item.DisplayName,
            item.Type,
            item.TypeLabel,
            canDeleteMembership ? item.MembershipResourceId : null,
            canDeleteMembership ? item.MembershipCassetteId : null);
    }

    private static bool CanDeleteMembership(
        string cassetteId,
        ProjectAccessSnapshot access)
    {
        if (access.HasProjectRight(ProjectRights.RebuildIndex))
        {
            return true;
        }

        return access.Cassettes.TryGetValue(cassetteId, out CassetteAccessSnapshot? snapshot) &&
            snapshot.Enabled &&
            snapshot.AllowWrite &&
            snapshot.Rights.Contains(CassetteRights.Delete);
    }
}
