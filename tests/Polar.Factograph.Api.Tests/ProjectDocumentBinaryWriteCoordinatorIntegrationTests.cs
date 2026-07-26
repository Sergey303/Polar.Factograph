using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectDocumentBinaryWriteCoordinatorIntegrationTests
{
    [Fact]
    public async Task AddAndReplaceAsync_PreserveUriAndQueuePreviewRequests()
    {
        await using WritableApiProjectFixture fixture =
            await WritableApiProjectFixture.CreateAsync();
        ProjectOperationGate gate = new();
        FileSystemCassetteDocumentWriter writer = new();
        FileSystemCassettePreviewRequestWriter previewWriter = new();
        IOptions<DocumentUploadOptions> options = Options.Create(new DocumentUploadOptions
        {
            MaxUploadBytes = 1024
        });
        ProjectDocumentAddCoordinator add = new(
            new ProjectCassetteCommandResolver(),
            gate,
            writer,
            previewWriter,
            options);
        ProjectDocumentReplaceCoordinator replace = new(
            new CassetteDocumentPathResolver(),
            new ProjectCassetteCommandResolver(),
            gate,
            writer,
            previewWriter,
            options);
        await using MemoryStream first = new("first"u8.ToArray(), writable: false);

        DocumentBinaryWriteResponse added = await add.AddAsync(
            fixture.Context,
            first,
            "scan.txt",
            requestedCassetteId: null,
            contentLength: first.Length);
        await using MemoryStream second = new("second"u8.ToArray(), writable: false);
        DocumentBinaryWriteResponse replaced = await replace.ReplaceAsync(
            fixture.Context,
            added.DocumentUri,
            second,
            "replacement.txt",
            contentLength: second.Length);

        Assert.False(added.Replaced);
        Assert.True(replaced.Replaced);
        Assert.Equal(added.DocumentUri, replaced.DocumentUri);
        Assert.Equal(PreviewQueueStates.Queued, added.PreviewState);
        Assert.Equal(PreviewQueueStates.Queued, replaced.PreviewState);
        Assert.NotNull(added.PreviewRequestId);
        Assert.NotNull(replaced.PreviewRequestId);
        Assert.NotEqual(added.PreviewRequestId, replaced.PreviewRequestId);
        CassetteDocumentLocation location = new CassetteDocumentPathResolver().Resolve(
            fixture.Project,
            replaced.DocumentUri);
        Assert.Equal("second", await File.ReadAllTextAsync(location.OriginalPath!));

        string cassettePath = fixture.Project.Cassettes.Single().Path;
        string queuePath = Path.Combine(cassettePath, "documents", "preview-queue");
        string[] requests = Directory.GetFiles(queuePath, "*.json");
        Assert.Equal(2, requests.Length);
        Assert.All(requests, request =>
            Assert.Contains(added.DocumentUri, File.ReadAllText(request), StringComparison.Ordinal));
    }
}
