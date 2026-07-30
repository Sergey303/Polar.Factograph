using Polar.Factograph.Api.Collections;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class CollectionContentsPresentationTests
{
    [Fact]
    public void Present_HidesMembershipInternalsFromViewer()
    {
        CollectionContentsResponse response = CollectionContentsPresentation.Present(
            Contents(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                membershipRights: Rights(CassetteRights.Read)));

        CollectionItemResponse item = Assert.Single(response.Items);
        Assert.Equal("resource-1", item.ResourceId);
        Assert.Null(item.MembershipResourceId);
        Assert.Null(item.MembershipCassetteId);
    }

    [Fact]
    public void Present_ExposesMembershipOnlyForEffectiveDeleteAccess()
    {
        CollectionContentsResponse response = CollectionContentsPresentation.Present(
            Contents(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                membershipRights: Rights(CassetteRights.Read, CassetteRights.Delete),
                membershipAllowWrite: true));

        CollectionItemResponse item = Assert.Single(response.Items);
        Assert.Equal("membership-1", item.MembershipResourceId);
        Assert.Equal("cassette-a", item.MembershipCassetteId);
    }

    [Fact]
    public void Present_DoesNotExposeMembershipWhenAllowWriteIsFalse()
    {
        CollectionContentsResponse response = CollectionContentsPresentation.Present(
            Contents(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                membershipRights: Rights(CassetteRights.Read, CassetteRights.Delete),
                membershipAllowWrite: false));

        CollectionItemResponse item = Assert.Single(response.Items);
        Assert.Null(item.MembershipResourceId);
        Assert.Null(item.MembershipCassetteId);
    }

    [Fact]
    public void Present_DoesNotUseDeleteRightFromAnotherCassette()
    {
        CollectionContentsResponse response = CollectionContentsPresentation.Present(
            Contents(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                membershipRights: Rights(CassetteRights.Read),
                otherRights: Rights(CassetteRights.Read, CassetteRights.Delete)));

        CollectionItemResponse item = Assert.Single(response.Items);
        Assert.Null(item.MembershipResourceId);
        Assert.Null(item.MembershipCassetteId);
    }

    [Fact]
    public void Present_ExposesMembershipToAdministrator()
    {
        CollectionContentsResponse response = CollectionContentsPresentation.Present(
            Contents(),
            Access(
                projectRights: Rights(ProjectRights.Read, ProjectRights.RebuildIndex),
                membershipRights: Rights(CassetteRights.Read)));

        CollectionItemResponse item = Assert.Single(response.Items);
        Assert.Equal("membership-1", item.MembershipResourceId);
        Assert.Equal("cassette-a", item.MembershipCassetteId);
    }

    private static ProjectCollectionContents Contents() => new(
        "collection-1",
        [
            new ProjectCollectionItem(
                "membership-1",
                "resource-1",
                "Элемент",
                "document",
                "Документ",
                "cassette-a",
                "cassette-b")
        ]);

    private static ProjectAccessSnapshot Access(
        IReadOnlySet<string> projectRights,
        IReadOnlySet<string> membershipRights,
        bool membershipAllowWrite = false,
        IReadOnlySet<string>? otherRights = null) => new(
        "user",
        IsMember: true,
        projectRights,
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cassette-a"] = new CassetteAccessSnapshot(
                "cassette-a",
                Enabled: true,
                AllowWrite: membershipAllowWrite,
                membershipRights),
            ["cassette-b"] = new CassetteAccessSnapshot(
                "cassette-b",
                Enabled: true,
                AllowWrite: otherRights?.Contains(CassetteRights.Delete) ?? false,
                otherRights ?? Rights(CassetteRights.Read))
        },
        DefaultWriteCassetteId: null);

    private static IReadOnlySet<string> Rights(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
