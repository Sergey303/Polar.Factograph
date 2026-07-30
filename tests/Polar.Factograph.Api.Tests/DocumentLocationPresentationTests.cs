using Polar.Factograph.Api.Documents;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class DocumentLocationPresentationTests
{
    [Fact]
    public void Present_HidesCassetteFromReadOnlyViewer()
    {
        DocumentLocationResponse response = DocumentLocationPresentation.Present(
            Location(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                sourceRights: Rights(CassetteRights.Read)));

        Assert.Null(response.CassetteId);
        Assert.Null(response.CassetteName);
        Assert.True(response.OriginalAvailable);
        Assert.True(response.IconPreviewAvailable);
        Assert.True(response.SmallPreviewAvailable);
        Assert.False(response.MediumPreviewAvailable);
        Assert.True(response.NormalPreviewAvailable);
    }

    [Fact]
    public void Present_ExposesCassetteToDocumentReplacer()
    {
        DocumentLocationResponse response = DocumentLocationPresentation.Present(
            Location(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                sourceRights: Rights(
                    CassetteRights.Read,
                    CassetteRights.ReplaceDocuments)));

        Assert.Equal("cassette-a", response.CassetteId);
        Assert.Equal("Archive", response.CassetteName);
    }

    [Fact]
    public void Present_DoesNotUseReplaceRightFromAnotherCassette()
    {
        DocumentLocationResponse response = DocumentLocationPresentation.Present(
            Location(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                sourceRights: Rights(CassetteRights.Read),
                otherRights: Rights(
                    CassetteRights.Read,
                    CassetteRights.ReplaceDocuments)));

        Assert.Null(response.CassetteId);
        Assert.Null(response.CassetteName);
    }

    [Fact]
    public void Present_ExposesCassetteToIndexAdministrator()
    {
        DocumentLocationResponse response = DocumentLocationPresentation.Present(
            Location(),
            Access(
                projectRights: Rights(
                    ProjectRights.Read,
                    ProjectRights.RebuildIndex),
                sourceRights: Rights(CassetteRights.Read)));

        Assert.Equal("cassette-a", response.CassetteId);
        Assert.Equal("Archive", response.CassetteName);
    }

    private static CassetteDocumentLocation Location() => new(
        "cassette-a",
        "Archive",
        "iiss://Archive@iis.nsk.su/0001/0042",
        "0001",
        "0042",
        OriginalPath: "original.jpg",
        SmallPreviewPath: "small.jpg",
        MediumPreviewPath: null,
        NormalPreviewPath: "normal.jpg")
    {
        IconPreviewPath = "icon.jpg"
    };

    private static ProjectAccessSnapshot Access(
        IReadOnlySet<string> projectRights,
        IReadOnlySet<string> sourceRights,
        IReadOnlySet<string>? otherRights = null) => new(
        "user",
        IsMember: true,
        projectRights,
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cassette-a"] = new CassetteAccessSnapshot(
                "cassette-a",
                Enabled: true,
                AllowWrite: sourceRights.Contains(CassetteRights.ReplaceDocuments),
                sourceRights),
            ["cassette-b"] = new CassetteAccessSnapshot(
                "cassette-b",
                Enabled: true,
                AllowWrite: otherRights?.Contains(CassetteRights.ReplaceDocuments) ?? false,
                otherRights ?? Rights(CassetteRights.Read))
        },
        DefaultWriteCassetteId: null);

    private static IReadOnlySet<string> Rights(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
