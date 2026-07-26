using System.Text;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Tests;

internal sealed class WritableApiProjectFixture(
    string root,
    ProjectDefinition project,
    ProjectAccessContext context) : IAsyncDisposable
{
    public string Root { get; } = root;
    public ProjectDefinition Project { get; } = project;
    public ProjectAccessContext Context { get; } = context;

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
            WritableApiProjectFog.Xml,
            new UTF8Encoding(false));

        ProjectDefinition project =
            WritableApiProjectDefinitionFactory.CreateProject(root, cassettePath);
        return new WritableApiProjectFixture(
            root,
            project,
            WritableApiProjectDefinitionFactory.CreateContext(project));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
