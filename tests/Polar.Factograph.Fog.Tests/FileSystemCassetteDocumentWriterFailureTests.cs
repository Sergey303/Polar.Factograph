using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassetteDocumentWriterFailureTests
{
    [Fact]
    public async Task AddAsync_OverLimitLeavesNoDocumentOrTemporaryFile()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream content = WritableDocumentCassette.Content("too-long");

        await Assert.ThrowsAsync<InvalidDataException>(() => writer.AddAsync(
            cassette.Definition,
            content,
            "file.txt",
            maxBytes: 3));

        string originals = Path.Combine(cassette.Root, "originals");
        Assert.Empty(Directory.EnumerateFiles(originals, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AddAsync_RejectsEmptyContent()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream content = new();

        await Assert.ThrowsAsync<InvalidDataException>(() => writer.AddAsync(
            cassette.Definition,
            content,
            "file.txt",
            maxBytes: 1024));
    }
}
