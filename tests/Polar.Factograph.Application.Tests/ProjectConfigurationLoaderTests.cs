using System.Text;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectConfigurationLoaderTests
{
    [Fact]
    public async Task LoadAsync_ResolvesPathsAndAcceptsValidAccessConfiguration()
    {
        await using TemporaryProject project = await TemporaryProject.CreateAsync(ValidJson);

        ProjectDefinition definition = await new ProjectConfigurationLoader().LoadAsync(project.Path);

        Assert.Equal("project", definition.ProjectId);
        Assert.True(Path.IsPathRooted(definition.Ontology.Path));
        Assert.True(Path.IsPathRooted(definition.Index.Path));
        Assert.True(Path.IsPathRooted(Assert.Single(definition.Cassettes).Path));
        Assert.Equal("current", definition.WriteRouting.DefaultCassetteByRole["editor"]);
    }

    [Theory]
    [InlineData("unknownProjectRight", "Unknown right 'unknownProjectRight'")]
    [InlineData("unknownCassette", "Unknown cassette 'missing'")]
    [InlineData("duplicateMember", "Duplicate project member: user")]
    [InlineData("invalidRoute", "targets non-writable cassette 'history'")]
    public async Task LoadAsync_RejectsInvalidAccessConfiguration(
        string scenario,
        string expectedMessage)
    {
        string json = scenario switch
        {
            "unknownProjectRight" => ValidJson.Replace(
                "\"projectRights\": [\"read\", \"search\"]",
                "\"projectRights\": [\"read\", \"unknownProjectRight\"]",
                StringComparison.Ordinal),
            "unknownCassette" => ValidJson.Replace(
                "\"current\": [\"read\", \"writeMetadata\"]",
                "\"missing\": [\"read\", \"writeMetadata\"]",
                StringComparison.Ordinal),
            "duplicateMember" => ValidJson.Replace(
                "{ \"userId\": \"user\", \"roles\": [\"editor\"] }",
                "{ \"userId\": \"user\", \"roles\": [\"editor\"] },\n    { \"userId\": \"user\", \"roles\": [\"viewer\"] }",
                StringComparison.Ordinal),
            "invalidRoute" => ValidJson.Replace(
                "\"editor\": \"current\"",
                "\"editor\": \"history\"",
                StringComparison.Ordinal),
            _ => throw new InvalidOperationException($"Unknown test scenario: {scenario}")
        };
        await using TemporaryProject project = await TemporaryProject.CreateAsync(json);

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => new ProjectConfigurationLoader().LoadAsync(project.Path));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
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

    private const string ValidJson = """
        {
          "schemaVersion": 1,
          "projectId": "project",
          "name": "Project",
          "ontology": { "path": "ontology.xml" },
          "index": { "path": "index" },
          "cassettes": [
            {
              "id": "history",
              "name": "History",
              "path": "history",
              "enabled": true,
              "defaultAccess": "read",
              "allowWrite": false
            },
            {
              "id": "current",
              "name": "Current",
              "path": "current",
              "enabled": true,
              "defaultAccess": "none",
              "allowWrite": true
            }
          ],
          "roles": {
            "viewer": {
              "projectRights": ["read", "search"],
              "cassetteRights": {}
            },
            "editor": {
              "projectRights": ["read", "search"],
              "cassetteRights": {
                "current": ["read", "writeMetadata"]
              }
            }
          },
          "members": [
            { "userId": "user", "roles": ["editor"] }
          ],
          "writeRouting": {
            "defaultCassetteByRole": {
              "editor": "current"
            }
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
            await File.WriteAllTextAsync(
                path,
                content,
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