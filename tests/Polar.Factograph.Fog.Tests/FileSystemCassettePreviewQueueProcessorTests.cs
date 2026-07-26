using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassettePreviewQueueProcessorTests
{
    [Fact]
    public async Task ProcessNextAsync_CompletesClaimedRequest()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        await QueueAsync(cassette);
        RecordingRenderer renderer = new();
        FileSystemCassettePreviewQueueProcessor processor = new();

        CassettePreviewProcessResult result = await processor.ProcessNextAsync(
            cassette.Definition,
            renderer);

        Assert.Equal(PreviewProcessStates.Completed, result.State);
        Assert.Equal(1, result.Attempt);
        Assert.Single(renderer.Requests);
        ProjectPreviewQueueStatus status = new CassettePreviewQueueStatusReader().Read(
            cassette.Project);
        Assert.Equal(0, status.Queued);
        Assert.Equal(0, status.Processing);
        Assert.Equal(0, status.Failed);
    }

    [Fact]
    public async Task ProcessNextAsync_RetriesTemporaryFailureThenCompletes()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        await QueueAsync(cassette);
        RecordingRenderer renderer = new(call =>
            call == 1 ? new IOException("temporary renderer failure") : null);
        FileSystemCassettePreviewQueueProcessor processor = new();
        CassettePreviewProcessingOptions options = new()
        {
            MaxAttempts = 3,
            RetryDelay = TimeSpan.Zero
        };

        CassettePreviewProcessResult first = await processor.ProcessNextAsync(
            cassette.Definition,
            renderer,
            options);
        CassettePreviewProcessResult second = await processor.ProcessNextAsync(
            cassette.Definition,
            renderer,
            options);

        Assert.Equal(PreviewProcessStates.Retried, first.State);
        Assert.Equal(1, first.Attempt);
        Assert.Equal(PreviewProcessStates.Completed, second.State);
        Assert.Equal(2, second.Attempt);
        Assert.Equal(1, renderer.Requests[1].Attempt);
    }

    [Fact]
    public async Task ProcessNextAsync_MovesPermanentFailureToDeadLetter()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        await QueueAsync(cassette);
        RecordingRenderer renderer = new(_ =>
            new PreviewRenderingException("unsupported document", retryable: false));
        FileSystemCassettePreviewQueueProcessor processor = new();

        CassettePreviewProcessResult result = await processor.ProcessNextAsync(
            cassette.Definition,
            renderer);

        Assert.Equal(PreviewProcessStates.Failed, result.State);
        Assert.Equal(1, result.Attempt);
        ProjectPreviewQueueStatus status = new CassettePreviewQueueStatusReader().Read(
            cassette.Project);
        Assert.Equal(1, status.Failed);
        string failedPath = Directory.GetFiles(
            Path.Combine(cassette.Root, "documents", "preview-failed"),
            "*.json").Single();
        string failedJson = await File.ReadAllTextAsync(failedPath);
        Assert.Contains("\"attempt\": 1", failedJson, StringComparison.Ordinal);
        Assert.Contains("unsupported document", failedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessNextAsync_QuarantinesInvalidRequest()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        string queuePath = Path.Combine(cassette.Root, "documents", "preview-queue");
        Directory.CreateDirectory(queuePath);
        await File.WriteAllTextAsync(Path.Combine(queuePath, "broken.json"), "{ broken");
        RecordingRenderer renderer = new();
        FileSystemCassettePreviewQueueProcessor processor = new();

        CassettePreviewProcessResult result = await processor.ProcessNextAsync(
            cassette.Definition,
            renderer);

        Assert.Equal(PreviewProcessStates.Invalid, result.State);
        Assert.Empty(renderer.Requests);
        ProjectPreviewQueueStatus status = new CassettePreviewQueueStatusReader().Read(
            cassette.Project);
        Assert.Equal(0, status.Queued);
        Assert.Equal(1, status.Failed);
        Assert.Single(Directory.GetFiles(
            Path.Combine(cassette.Root, "documents", "preview-failed"),
            "*.error.txt"));
    }

    private static async Task QueueAsync(WritableDocumentCassette cassette)
    {
        CassetteDocumentWriteResult document = new(
            cassette.Definition.Id,
            cassette.Definition.Name,
            "iiss://Cassette@iis.nsk.su/0001/0001",
            "0001",
            "0001",
            "0001.pdf",
            10,
            new string('a', 64),
            Replaced: false);
        CassettePreviewQueueResult result = await new FileSystemCassettePreviewRequestWriter()
            .QueueAsync(cassette.Definition, document);
        Assert.Equal(PreviewQueueStates.Queued, result.State);
    }

    private sealed class RecordingRenderer(
        Func<int, Exception?>? failure = null) : ICassettePreviewRenderer
    {
        public List<CassettePreviewRequest> Requests { get; } = [];

        public Task RenderAsync(
            CassetteDefinition cassette,
            CassettePreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            Exception? exception = failure?.Invoke(Requests.Count);
            return exception is null
                ? Task.CompletedTask
                : Task.FromException(exception);
        }
    }
}