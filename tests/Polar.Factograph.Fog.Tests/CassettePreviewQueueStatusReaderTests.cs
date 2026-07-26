using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class CassettePreviewQueueStatusReaderTests
{
    [Fact]
    public async Task Read_UsesRequestTimestampInsteadOfMutableFileTime()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
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
        CassettePreviewQueueResult queued = await new FileSystemCassettePreviewRequestWriter()
            .QueueAsync(cassette.Definition, document);
        string requestPath = Directory.GetFiles(
            Path.Combine(cassette.Root, "documents", "preview-queue"),
            "*.json").Single();
        File.SetLastWriteTimeUtc(requestPath, DateTime.UtcNow.AddDays(1));

        ProjectPreviewQueueStatus status = new CassettePreviewQueueStatusReader().Read(
            cassette.Project);

        Assert.Equal(queued.QueuedAtUtc, status.Cassettes.Single().OldestQueuedAtUtc);
    }
}