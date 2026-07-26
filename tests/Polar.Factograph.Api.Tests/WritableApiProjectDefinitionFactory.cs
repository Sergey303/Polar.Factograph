using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Tests;

internal static class WritableApiProjectDefinitionFactory
{
    public static ProjectDefinition CreateProject(string root, string cassettePath) => new()
    {
        ProjectId = "write-test",
        Name = "Write test",
        Ontology = new OntologyDefinition { Path = Path.Combine(root, "ontology.xml") },
        Index = new IndexDefinition { Path = Path.Combine(root, "index") },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "current",
                Name = "Cassette",
                Path = cassettePath,
                Enabled = true,
                DefaultAccess = "read",
                AllowWrite = true
            }
        ]
    };

    public static ProjectAccessContext CreateContext(ProjectDefinition project)
    {
        HashSet<string> rights = new(StringComparer.Ordinal)
        {
            CassetteRights.Read,
            CassetteRights.WriteMetadata
        };
        ProjectAccessSnapshot access = new(
            "editor",
            IsMember: true,
            new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
            {
                ["current"] = new("current", true, true, rights)
            },
            DefaultWriteCassetteId: "current");
        return new ProjectAccessContext(project, access);
    }
}
