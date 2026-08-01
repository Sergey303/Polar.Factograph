using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectConfigurationLoaderTests
{
    [Fact]
    public async Task LoadAsync_ResolvesPathsAndCreatesBuiltInAccessConfiguration()
    {
        await using TemporaryProject project = await TemporaryProject.CreateAsync(ValidJson);

        ProjectDefinition definition = await new ProjectConfigurationLoader().LoadAsync(project.Path);

        Assert.Equal("project", definition.ProjectId);
        Assert.True(Path.IsPathRooted(definition.Ontology.Path));
        Assert.True(Path.IsPathRooted(definition.Index.Path));
        Assert.Equal(2, definition.Cassettes.Length);
        Assert.All(definition.Cassettes, cassette => Assert.True(Path.IsPathRooted(cassette.Path)));
        Assert.Equal(new[] { "history", "current" }, definition.Cassettes.Select(value => value.Id));

        CassetteDefinition writable = Assert.Single(definition.Cassettes, value => value.AllowWrite);
        Assert.Equal("current", writable.Id);
        Assert.Equal(writable.Id, writable.Name);

        Assert.Empty(definition.Members);
        Assert.Equal(
            new[] { "administrator", "editor", "viewer" },
            definition.Roles.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(
            new[] { ProjectRights.Read, ProjectRights.Search },
            definition.Roles["viewer"].ProjectRights);
        Assert.Contains(
            CassetteRights.WriteMetadata,
            definition.Roles["editor"].CassetteRights["current"]);
        Assert.Contains(
            CassetteRights.Manage,
            definition.Roles["administrator"].CassetteRights["*"]);
    }

    [Theory]
    [InlineData("roles", "{}")]
    [InlineData("members", "[]")]
    [InlineData("writeRouting", "{}")]
    public async Task LoadAsync_RejectsRemovedAccessSections(string name, string value)
    {
        string json = AddTopLevelSection(ValidJson, name, value);
        await using TemporaryProject project = await TemporaryProject.CreateAsync(json);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new ProjectConfigurationLoader().LoadAsync(project.Path));

        Assert.Contains(
            "is no longer supported",
            exception.InnerException?.Message ?? exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("writeOutsideItems", "must exactly match one path")]
    [InlineData("relativeItem", "must contain a full cassette path")]
    [InlineData("duplicateFolderName", "folder name must be unique")]
    public async Task LoadAsync_RejectsInvalidCassettePaths(
        string scenario,
        string expectedMessage)
    {
        string json = scenario switch
        {
            "writeOutsideItems" => ValidJson.Replace(
                "\"write\": \"__ROOT__/current\"",
                "\"write\": \"__ROOT__/other\"",
                StringComparison.Ordinal),
            "relativeItem" => ValidJson.Replace(
                "\"__ROOT__/history\"",
                "\"history\"",
                StringComparison.Ordinal),
            "duplicateFolderName" => ValidJson.Replace(
                "\"__ROOT__/current\"",
                "\"__ROOT__/nested/history\"",
                StringComparison.Ordinal),
            _ => throw new InvalidOperationException($"Unknown test scenario: {scenario}")
        };
        await using TemporaryProject project = await TemporaryProject.CreateAsync(json);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new ProjectConfigurationLoader().LoadAsync(project.Path));

        Assert.Contains(expectedMessage, exception.InnerException?.Message ?? exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadAsync_WrapsMalformedJsonAsProjectDataError()
    {
        await using TemporaryProject project = await TemporaryProject.CreateAsync("{ not-json }");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new ProjectConfigurationLoader().LoadAsync(project.Path));

        Assert.Contains("Project configuration JSON cannot be read", exception.Message, StringComparison.Ordinal);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
    }

    private static string AddTopLevelSection(string json, string name, string value)
    {
        int closingBrace = json.LastIndexOf('}');
        if (closingBrace < 0)
        {
            throw new InvalidOperationException("Test JSON has no closing object brace.");
        }

        return string.Concat(
            json.AsSpan(0, closingBrace),
            $",\n  \"{name}\": {value}\n",
            json.AsSpan(closingBrace));
    }

    private const string ValidJson = """
        {
          "schemaVersion": 1,
          "projectId": "project",
          "name": "Project",
          "ontology": { "path": "ontology.xml" },
          "index": { "path": "index" },
          "cassettes": {
            "items": [
              "__ROOT__/history",
              "__ROOT__/current"
            ],
            "write": "__ROOT__/current"
          }
        }
        """;

    private sealed class TemporaryProject : IAsyncDisposable
    {
        private TemporaryProject(string directory, string path)
        {
            Directory = directory;
            Path = path;
        }

        public string Directory { get; }
        public string Path { get; }

        public static async Task<TemporaryProject> CreateAsync(string content)
        {
            string directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "polar-factograph-project-tests",
                Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, "project.json");
            string root = directory.Replace('\\', '/');
            await File.WriteAllTextAsync(
                path,
                content.Replace("__ROOT__", root, StringComparison.Ordinal),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return new TemporaryProject(directory, path);
        }

        public ValueTask DisposeAsync()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
