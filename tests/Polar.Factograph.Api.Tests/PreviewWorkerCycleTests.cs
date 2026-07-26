using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Previews;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class PreviewWorkerCycleTests
{
    [Fact]
    public async Task RunAsync_WithOneItemLimit_RotatesBetweenCassettes()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-preview-worker-tests",
            Guid.NewGuid().ToString("N"));
        try
        {
            CassetteDefinition first = CreateCassette(root, "a");
            CassetteDefinition second = CreateCassette(root, "b");
            await QueueAsync(first, "0001");
            await QueueAsync(second, "0002");
            RecordingRenderer renderer = new();
            PreviewWorkerCycle worker = new(
                new FileSystemCassettePreviewQueueProcessor(),
                renderer,
                Options.Create(new PreviewWorkerOptions
                {
                    Enabled = true,
                    Executable = "unused",
                    MaxItemsPerCycle = 1,
                    RetryDelaySeconds = 0
                }));
            ProjectDefinition project = CreateProject(root, first, second);

            Assert.Equal(1, await worker.RunAsync(project));
            Assert.Equal(1, await worker.RunAsync(project));

            Assert.Equal(new[] { "a", "b" }, renderer.CassetteIds);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static CassetteDefinition CreateCassette(string root, string id)
    {
        string path = Path.Combine(root, id);
        Directory.CreateDirectory(path);
        return new CassetteDefinition
        {
            Id = id,
            Name = id.ToUpperInvariant(),
            Path = path,
            Enabled = true,
            AllowWrite = true
        };
    }

    private static ProjectDefinition CreateProject(
        string root,
        params CassetteDefinition[] cassettes) => new()
    {
        ProjectId = "preview-worker",
        Name = "Preview worker",
        Ontology = new OntologyDefinition { Path = Path.Combine(root, "ontology.xml") },
        Index = new IndexDefinition { Path = Path.Combine(root, "index") },
        Cassettes = cassettes
    };

    private static async Task QueueAsync(CassetteDefinition cassette, string number)
    {
        CassetteDocumentWriteResult document = new(
            cassette.Id,
            cassette.Name,
            $"iiss://{cassette.Name}@iis.nsk.su/0001/{number}",
            "0001",
            number,
            $"{number}.txt",
            10,
            new string('a', 64),
            Replaced: false);
        CassettePreviewQueueResult result = await new FileSystemCassettePreviewRequestWriter()
            .QueueAsync(cassette, document);
        Assert.Equal(PreviewQueueStates.Queued, result.State);
    }

    private sealed class RecordingRenderer : ICassettePreviewRenderer
    {
        public List<string> CassetteIds { get; } = [];

        public Task RenderAsync(
            CassetteDefinition cassette,
            CassettePreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            CassetteIds.Add(cassette.Id);
            return Task.CompletedTask;
        }
    }
}
