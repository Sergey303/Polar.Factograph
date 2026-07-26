using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassettePreviewRequestWriterTests
{
    [Fact]
    public async Task QueueAsync_WhenQueueCannotBeCreated_ReturnsFailureWithoutThrowing()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        await File.WriteAllTextAsync(
            Path.Combine(cassette.Root, "documents"),
            "blocks the preview queue directory");
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
        FileSystemCassettePreviewRequestWriter writer = new();

        CassettePreviewQueueResult result = await writer.QueueAsync(
            cassette.Definition,
            document);

        Assert.Equal(PreviewQueueStates.QueueFailed, result.State);
        Assert.Null(result.RequestId);
        Assert.Null(result.QueuedAtUtc);
    }
}
