using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writing;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal static class ResourceWriteTestData
{
    public static ProjectDefinition Project(string indexPath) => new()
    {
        ProjectId = "project",
        Name = "Project",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = indexPath },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "current",
                Name = "Current",
                Path = "cassette",
                AllowWrite = true
            }
        ]
    };

    public static ProjectAccessSnapshot Access() => new(
        "user",
        IsMember: true,
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["current"] = new(
                "current",
                Enabled: true,
                AllowWrite: true,
                new HashSet<string>(
                    [CassetteRights.WriteMetadata],
                    StringComparer.Ordinal))
        },
        DefaultWriteCassetteId: "current");

    public static FogSourceDescriptor Source() => new(
        "current",
        "Current",
        "current.fog",
        "database",
        "iiss://Current@iis.nsk.su",
        "owner",
        "p",
        1,
        Writable: true,
        IsCassetteMetadata: true,
        Length: 1,
        LastWriteTimeUtc: DateTime.UtcNow);

    public static ProjectResourceWriteCommand Command() => new(
        new FogResourceWriteRequest(
            "person",
            [new FogProperty("name", FogPropertyKind.Literal, "Alice")]),
        CassetteId: null);

    public static ProjectIndexRebuildResult RebuildResult(Guid generationId) => new(
        generationId,
        SourceFiles: 2,
        new ProjectIndexBuildStatistics(1, 3, 2, 4));
}
