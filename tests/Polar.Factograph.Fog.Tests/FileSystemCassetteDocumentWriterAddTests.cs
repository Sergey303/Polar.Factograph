using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassetteDocumentWriterAddTests
{
    [Fact]
    public async Task AddAsync_WritesCompatibleOriginalAndHash()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream content = WritableDocumentCassette.Content("document-one");

        CassetteDocumentWriteResult result = await writer.AddAsync(
            cassette.Definition,
            content,
            "Photo.JPG",
            maxBytes: 1024);

        Assert.Equal("iiss://Cassette@iis.nsk.su/0001/0001", result.DocumentUri);
        Assert.Equal("0001.jpg", result.FileName);
        Assert.Equal(12, result.Length);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("document-one")))
                .ToLowerInvariant(),
            result.Sha256);
        Assert.Equal("document-one", await File.ReadAllTextAsync(cassette.OriginalPath(result)));
        Assert.False(result.Replaced);
    }

    [Fact]
    public async Task AddAsync_AllocatesNextDocumentNumber()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream first = WritableDocumentCassette.Content("one");
        await using MemoryStream second = WritableDocumentCassette.Content("two");

        _ = await writer.AddAsync(cassette.Definition, first, "one.txt", 1024);
        CassetteDocumentWriteResult result = await writer.AddAsync(
            cassette.Definition,
            second,
            "two.txt",
            1024);

        Assert.Equal("0001", result.FolderName);
        Assert.Equal("0002", result.DocumentNumber);
        Assert.Equal("0002.txt", result.FileName);
    }

    [Fact]
    public async Task AddAsync_DoesNotReuseNumberHeldByOrphanPreview()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        string previewDirectory = Path.Combine(
            cassette.Root,
            "documents",
            "small",
            "0001");
        Directory.CreateDirectory(previewDirectory);
        await File.WriteAllTextAsync(Path.Combine(previewDirectory, "0002.jpg"), "old-preview");
        string originalsDirectory = Path.Combine(cassette.Root, "originals", "0001");
        Directory.CreateDirectory(originalsDirectory);
        await File.WriteAllTextAsync(Path.Combine(originalsDirectory, "0001.jpg"), "old-original");
        FileSystemCassetteDocumentWriter writer = new();
        await using MemoryStream content = WritableDocumentCassette.Content("new-original");

        CassetteDocumentWriteResult result = await writer.AddAsync(
            cassette.Definition,
            content,
            "new.png",
            1024);

        Assert.Equal("0001", result.FolderName);
        Assert.Equal("0003", result.DocumentNumber);
        Assert.Equal("0003.png", result.FileName);
    }
}
