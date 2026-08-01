using System.Text;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectConfigurationBomTests
{
    [Fact]
    public async Task LoadAsync_AcceptsUtf8Bom()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-project-bom-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            string projectPath = Path.Combine(directory, "project.json");
            string cassettePath = Path.Combine(directory, "cassette").Replace('\\', '/');
            string json = $$"""
                {
                  "schemaVersion": 1,
                  "projectId": "bom-project",
                  "name": "BOM project",
                  "homeResourceId": "home",
                  "ontology": { "path": "ontology.xml" },
                  "index": { "path": "index" },
                  "cassettes": {
                    "items": ["{{cassettePath}}"],
                    "write": "{{cassettePath}}"
                  }
                }
                """;
            await File.WriteAllTextAsync(
                projectPath,
                json,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            Domain.ProjectDefinition project =
                await new ProjectConfigurationLoader().LoadAsync(projectPath);

            Assert.Equal("bom-project", project.ProjectId);
            Assert.Equal("home", project.HomeResourceId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
