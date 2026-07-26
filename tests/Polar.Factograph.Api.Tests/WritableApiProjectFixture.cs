using System.Text;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Tests;

internal sealed class WritableApiProjectFixture : IAsyncDisposable
{
    private WritableApiProjectFixture(
        string root,
        ProjectDefinition project,
        ProjectAccessContext context)
    {
        Root = root;
        Project = project;
        Context = context;
    }

    public string Root { get; }
    public ProjectDefinition Project { get; }
    public ProjectAccessContext Context { get; }

    public static async Task<WritableApiProjectFixture> CreateAsync()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-write-api-tests",
            Guid.NewGuid().ToString("N"));
        string cassettePath = Path.Combine(root, "Cassette");
        string metaPath = Path.Combine(cassettePath, "meta");
        Directory.CreateDirectory(metaPath);
        await File.WriteAllTextAsync(
            Path.Combine(metaPath, "Cassette_current.fog"),
            FogXml,
            new UTF8Encoding(false));

        ProjectDefinition project = new()
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

        return new WritableApiProjectFixture(
            root,
            project,
            new ProjectAccessContext(project, access));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        return ValueTask.CompletedTask;
    }

    private const string FogXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
                 dbid="write-test" owner="editor" prefix="p" counter="1">
          <person rdf:about="existing" mT="2020-01-01 00:00:00Z">
            <name>Existing</name>
          </person>
        </rdf:RDF>
        """;
}
