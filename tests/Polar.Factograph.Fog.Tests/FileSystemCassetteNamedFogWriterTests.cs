using Xunit;

namespace Polar.Factograph.Fog.Tests;

public sealed class FileSystemCassetteNamedFogWriterTests
{
    [Fact]
    public async Task AddAsync_keeps_document_number_and_unicode_login_in_filename()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteNamedFogWriter writer = new();
        await using MemoryStream content = WritableDocumentCassette.Content("<rdf:RDF />");

        CassetteDocumentWriteResult result = await writer.AddAsync(
            cassette.Definition,
            content,
            "Сергей.fog",
            maxBytes: 1024);

        Assert.Equal("iiss://Cassette@iis.nsk.su/0001/0001", result.DocumentUri);
        Assert.Equal("0001-Сергей.fog", result.FileName);
        Assert.True(File.Exists(cassette.OriginalPath(result)));
    }

    [Fact]
    public async Task Regular_document_allocation_continues_after_named_fog()
    {
        using WritableDocumentCassette cassette = WritableDocumentCassette.Create();
        FileSystemCassetteNamedFogWriter fogWriter = new();
        FileSystemCassetteDocumentWriter documentWriter = new();
        await using MemoryStream fog = WritableDocumentCassette.Content("<rdf:RDF />");
        await using MemoryStream document = WritableDocumentCassette.Content("document");

        _ = await fogWriter.AddAsync(
            cassette.Definition,
            fog,
            "Пользователь.fog",
            maxBytes: 1024);
        CassetteDocumentWriteResult result = await documentWriter.AddAsync(
            cassette.Definition,
            document,
            "document.txt",
            maxBytes: 1024);

        Assert.Equal("0002", result.DocumentNumber);
        Assert.Equal("0002.txt", result.FileName);
    }
}
