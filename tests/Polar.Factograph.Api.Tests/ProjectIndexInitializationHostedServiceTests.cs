using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectIndexInitializationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_BuildsMissingCurrentGenerationOnlyOnce()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-index-startup-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            string cassettePath = Path.Combine(root, "cassette");
            string metadataPath = Path.Combine(cassettePath, "meta");
            Directory.CreateDirectory(metadataPath);
            await File.WriteAllTextAsync(
                Path.Combine(metadataPath, "Demo_current.fog"),
                FogXml,
                new UTF8Encoding(false));
            await File.WriteAllTextAsync(
                Path.Combine(root, "ontology.xml"),
                "<Ontology />",
                new UTF8Encoding(false));
            string projectPath = Path.Combine(root, "project.json");
            await File.WriteAllTextAsync(
                projectPath,
                ProjectJson,
                new UTF8Encoding(false));

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Project:ConfigPath"] = projectPath
                })
                .Build();
            ProjectIndexDirtyMarker dirtyMarker = new();
            ProjectIndexCoordinator coordinator = CreateCoordinator(dirtyMarker);
            ProjectIndexInitializationHostedService service = new(
                new ProjectPathResolver(configuration, new TestHostEnvironment(root)),
                new ProjectConfigurationLoader(),
                coordinator,
                dirtyMarker,
                NullLogger<ProjectIndexInitializationHostedService>.Instance);

            await service.StartAsync(CancellationToken.None);
            string indexRoot = Path.Combine(root, "index");
            string firstGeneration = Assert.IsType<string>(
                FileSystemIndexGeneration.GetCurrentGenerationPath(indexRoot));

            await service.StartAsync(CancellationToken.None);
            string secondGeneration = Assert.IsType<string>(
                FileSystemIndexGeneration.GetCurrentGenerationPath(indexRoot));

            Assert.Equal(firstGeneration, secondGeneration);
            Assert.True(Directory.Exists(firstGeneration));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static ProjectIndexCoordinator CreateCoordinator(
        ProjectIndexDirtyMarker dirtyMarker)
    {
        FileSystemFogSourceScanner scanner = new();
        return new ProjectIndexCoordinator(
            scanner,
            new FogProjectRecordSource(new FileSystemFogRecordReader()),
            new LegacyFogProjectMaterializer(),
            new ProjectIndexRebuilder(),
            new ProjectOperationGate(),
            dirtyMarker);
    }

    private const string ProjectJson = """
        {
          "schemaVersion": 1,
          "projectId": "startup-test",
          "name": "Startup test",
          "ontology": { "path": "./ontology.xml" },
          "index": { "path": "./index", "rebuildMode": "whenSourcesChanged" },
          "cassettes": [
            {
              "id": "demo",
              "name": "Demo",
              "path": "./cassette",
              "enabled": true,
              "defaultAccess": "read",
              "allowWrite": false
            }
          ]
        }
        """;

    private const string FogXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <rdf:RDF
          xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
          xmlns:fog="http://fogid.net/o/"
          dbid="Demo_current"
          uri="iiss://Demo@test"
          owner="test">
          <fog:person rdf:about="person-1">
            <fog:name xml:lang="ru">Марчук</fog:name>
          </fog:person>
        </rdf:RDF>
        """;

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
