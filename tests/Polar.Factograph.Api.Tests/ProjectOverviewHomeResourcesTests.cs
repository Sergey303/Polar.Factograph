using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectOverviewHomeResourcesTests
{
    [Fact]
    public void Present_ExposesTrimmedDistinctHomeResourceIds()
    {
        ProjectDefinition project = new()
        {
            ProjectId = "project",
            Name = "Archive",
            Ontology = new OntologyDefinition { Path = "ontology.xml" },
            Index = new IndexDefinition { Path = "index" },
            HomeResourceIds = [" collection-1 ", "", "collection-1", "photo-1"]
        };
        ProjectAccessSnapshot access = new(
            "viewer",
            IsMember: true,
            new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal),
            DefaultWriteCassetteId: null);

        ProjectOverview overview = ProjectOverviewPresentation.Present(project, access);

        Assert.Equal(new[] { "collection-1", "photo-1" }, overview.HomeResourceIds);
    }
}
