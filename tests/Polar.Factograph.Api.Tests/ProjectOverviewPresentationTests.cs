using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectOverviewPresentationTests
{
    [Fact]
    public void Present_HidesCassetteAndRawRightsFromViewer()
    {
        ProjectOverview overview = ProjectOverviewPresentation.Present(
            Project(),
            Access(
                projectRights: Rights(ProjectRights.Read, ProjectRights.Search),
                cassetteA: Rights(CassetteRights.Read),
                cassetteB: Rights(CassetteRights.Read),
                defaultCassetteId: "cassette-a"));

        Assert.Equal("project", overview.ProjectId);
        Assert.Equal("Archive", overview.Name);
        Assert.False(overview.CanAdmin);
        Assert.Empty(overview.Cassettes);
        Assert.Null(overview.DefaultWriteCassetteId);
    }

    [Fact]
    public void Present_ExposesOnlyWritableCassetteAndWriteRightsToEditor()
    {
        ProjectOverview overview = ProjectOverviewPresentation.Present(
            Project(),
            Access(
                projectRights: Rights(ProjectRights.Read, ProjectRights.Search),
                cassetteA: Rights(
                    CassetteRights.Read,
                    CassetteRights.WriteMetadata,
                    CassetteRights.AddDocuments),
                cassetteB: Rights(CassetteRights.Read),
                defaultCassetteId: "cassette-a"));

        ProjectCassetteOverview cassette = Assert.Single(overview.Cassettes);
        Assert.Equal("cassette-a", cassette.Id);
        Assert.Equal("Writable", cassette.Name);
        Assert.True(cassette.AllowWrite);
        Assert.Equal(
            new[] { CassetteRights.AddDocuments, CassetteRights.WriteMetadata },
            cassette.Rights);
        Assert.Equal("cassette-a", overview.DefaultWriteCassetteId);
        Assert.False(overview.CanAdmin);
    }

    [Fact]
    public void Present_ExposesAllReadableCassetteRightsToAdministrator()
    {
        ProjectOverview overview = ProjectOverviewPresentation.Present(
            Project(),
            Access(
                projectRights: Rights(
                    ProjectRights.Read,
                    ProjectRights.Search,
                    ProjectRights.RebuildIndex),
                cassetteA: Rights(
                    CassetteRights.Read,
                    CassetteRights.WriteMetadata),
                cassetteB: Rights(CassetteRights.Read),
                defaultCassetteId: "cassette-a"));

        Assert.True(overview.CanAdmin);
        Assert.Equal(2, overview.Cassettes.Count);
        Assert.Contains(overview.Cassettes, cassette =>
            cassette.Id == "cassette-a" &&
            cassette.Rights.Contains(CassetteRights.Read) &&
            cassette.Rights.Contains(CassetteRights.WriteMetadata));
        Assert.Contains(overview.Cassettes, cassette =>
            cassette.Id == "cassette-b" &&
            cassette.Rights.SequenceEqual([CassetteRights.Read]));
        Assert.Equal("cassette-a", overview.DefaultWriteCassetteId);
    }

    [Fact]
    public void Present_DropsDefaultCassetteWhenItIsNotExposed()
    {
        ProjectOverview overview = ProjectOverviewPresentation.Present(
            Project(),
            Access(
                projectRights: Rights(ProjectRights.Read),
                cassetteA: Rights(CassetteRights.Read),
                cassetteB: Rights(
                    CassetteRights.Read,
                    CassetteRights.WriteMetadata),
                defaultCassetteId: "cassette-a"));

        ProjectCassetteOverview cassette = Assert.Single(overview.Cassettes);
        Assert.Equal("cassette-b", cassette.Id);
        Assert.Null(overview.DefaultWriteCassetteId);
    }

    private static ProjectDefinition Project() => new()
    {
        ProjectId = "project",
        Name = "Archive",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = "index" },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "cassette-a",
                Name = "Writable",
                Path = "a",
                Enabled = true,
                AllowWrite = true
            },
            new CassetteDefinition
            {
                Id = "cassette-b",
                Name = "Read only",
                Path = "b",
                Enabled = true,
                AllowWrite = true
            },
            new CassetteDefinition
            {
                Id = "cassette-disabled",
                Name = "Disabled",
                Path = "disabled",
                Enabled = false,
                AllowWrite = true
            }
        ]
    };

    private static ProjectAccessSnapshot Access(
        IReadOnlySet<string> projectRights,
        IReadOnlySet<string> cassetteA,
        IReadOnlySet<string> cassetteB,
        string? defaultCassetteId) => new(
        "user",
        IsMember: true,
        projectRights,
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cassette-a"] = new CassetteAccessSnapshot(
                "cassette-a",
                Enabled: true,
                AllowWrite: true,
                cassetteA),
            ["cassette-b"] = new CassetteAccessSnapshot(
                "cassette-b",
                Enabled: true,
                AllowWrite: true,
                cassetteB),
            ["cassette-disabled"] = new CassetteAccessSnapshot(
                "cassette-disabled",
                Enabled: false,
                AllowWrite: true,
                Rights(CassetteRights.Read, CassetteRights.WriteMetadata))
        },
        defaultCassetteId);

    private static IReadOnlySet<string> Rights(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
