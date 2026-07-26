using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassetteDocumentWriterReplaceTests
{
    [Fact]
    public async Task ReplaceAsync_PreservesUriAndReplacesOriginal()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream initial = WritableDocumentCassette.Content("old");
        CassetteDocumentWriteResult added = await writer.AddAsync(
            cassette.Definition,
            initial,
            "scan.pdf",
            1024);
        CassetteDocumentLocation location = new CassetteDocumentPathResolver().Resolve(
            cassette.Project,
            added.DocumentUri);
        await using MemoryStream replacement = WritableDocumentCassette.Content("new-content");

        CassetteDocumentWriteResult replaced = await writer.ReplaceAsync(
            cassette.Definition,
            location,
            replacement,
            "replacement.PDF",
            1024);

        Assert.True(replaced.Replaced);
        Assert.Equal(added.DocumentUri, replaced.DocumentUri);
        Assert.Equal(added.FileName, replaced.FileName);
        Assert.Equal("new-content", await File.ReadAllTextAsync(cassette.OriginalPath(replaced)));
    }

    [Fact]
    public async Task ReplaceAsync_RejectsDifferentExtensionWithoutChangingFile()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream initial = WritableDocumentCassette.Content("old");
        CassetteDocumentWriteResult added = await writer.AddAsync(
            cassette.Definition,
            initial,
            "scan.pdf",
            1024);
        CassetteDocumentLocation location = new CassetteDocumentPathResolver().Resolve(
            cassette.Project,
            added.DocumentUri);
        await using MemoryStream replacement = WritableDocumentCassette.Content("new");

        await Assert.ThrowsAsync<ArgumentException>(() => writer.ReplaceAsync(
            cassette.Definition,
            location,
            replacement,
            "scan.txt",
            1024));

        Assert.Equal("old", await File.ReadAllTextAsync(cassette.OriginalPath(added)));
    }
}
