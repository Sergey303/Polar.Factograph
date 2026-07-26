using System.Text;
using System.Xml;

namespace Polar.Factograph.Fog;

internal sealed record FogRewriteOutcome(
    string ResourceId,
    long NextCounter);

internal static class FogResourceFileRewriter
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Async = true,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = true,
        CloseOutput = false
    };

    public static async Task<FogRewriteOutcome> RewriteAsync(
        string sourcePath,
        string temporaryPath,
        FogResourceWriteRequest request,
        DateTime modifiedAtUtc,
        CancellationToken cancellationToken)
    {
        await using FileStream input = OpenInput(sourcePath);
        using XmlReader reader = FogXmlReaderFactory.Create(input);
        await using FileStream output = OpenOutput(temporaryPath);
        using XmlWriter writer = XmlWriter.Create(output, WriterSettings);

        await writer.WriteStartDocumentAsync();
        XmlNodeType content = await reader.MoveToContentAsync();
        if (content != XmlNodeType.Element)
        {
            throw new InvalidDataException($"Fog file is empty: {sourcePath}");
        }

        FogWriteRootState root = FogWriteRootState.Read(reader, request, sourcePath);
        await FogXmlRootWriter.WriteStartAsync(reader, writer, root.WrittenCounter);
        await FogExistingRecordCopier.CopyAsync(
            reader,
            writer,
            sourcePath,
            cancellationToken);
        FogResourceElementFactory.Create(
            request,
            root.ResourceId,
            modifiedAtUtc).WriteTo(writer);
        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
        await output.FlushAsync(cancellationToken);
        output.Flush(flushToDisk: true);

        return new FogRewriteOutcome(root.ResourceId, root.NextCounter);
    }

    private static FileStream OpenInput(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 64 * 1024,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    private static FileStream OpenOutput(string path) => new(
        path,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None,
        bufferSize: 64 * 1024,
        FileOptions.Asynchronous | FileOptions.SequentialScan);
}
