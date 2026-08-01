using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectOverviewHomeResourceTests
{
    [Fact]
    public void Present_ExposesTrimmedHomeResourceId()
    {
        ProjectDefinition project = new()
        {
            ProjectId = "project",
            Name = "Archive",
            Ontology = new OntologyDefinition { Path = "ontology.xml" },
            Index = new IndexDefinition { Path = "index" },
            HomeResourceId = " featured-collection "
        };
        ProjectAccessSnapshot access = new(
            "viewer",
            IsMember: true,
            new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal),
            DefaultWriteCassetteId: null);

        ProjectOverview overview = ProjectOverviewPresentation.Present(project, access);

        Assert.Equal("featured-collection", overview.HomeResourceId);
    }

    [Fact]
    public void Present_TreatsBlankHomeResourceIdAsMissing()
    {
        ProjectDefinition project = new()
        {
            ProjectId = "project",
            Name = "Archive",
            Ontology = new OntologyDefinition { Path = "ontology.xml" },
            Index = new IndexDefinition { Path = "index" },
            HomeResourceId = "   "
        };
        ProjectAccessSnapshot access = new(
            "viewer",
            IsMember: true,
            new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal),
            DefaultWriteCassetteId: null);

        ProjectOverview overview = ProjectOverviewPresentation.Present(project, access);

        Assert.Null(overview.HomeResourceId);
    }
}
