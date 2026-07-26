using System.Xml;

namespace Polar.Factograph.Fog;

internal sealed record FogRewriteOutcome(
    string ResourceId,
    long NextCounter,
    DateTime ModifiedAtUtc);

internal static class FogResourceFileRewriter
{
    public static async Task<FogRewriteOutcome> RewriteAsync(
        string sourcePath,
        string temporaryPath,
        FogResourceWriteRequest request,
        DateTime requestedModifiedAtUtc,
        CancellationToken cancellationToken)
    {
        await using FileStream input = FogRewriteStreamFactory.OpenInput(sourcePath);
        using XmlReader reader = FogXmlReaderFactory.Create(input);
        await using FileStream output = FogRewriteStreamFactory.OpenOutput(temporaryPath);
        using XmlWriter writer = FogRewriteStreamFactory.CreateWriter(output);

        await writer.WriteStartDocumentAsync();
        XmlNodeType content = await reader.MoveToContentAsync();
        if (content != XmlNodeType.Element)
        {
            throw new InvalidDataException($"Fog file is empty: {sourcePath}");
        }

        FogWriteRootState root = FogWriteRootState.Read(reader, request, sourcePath);
        await FogXmlRootWriter.WriteStartAsync(reader, writer, root.WrittenCounter);
        DateTime? latestModifiedAt = await FogExistingRecordCopier.CopyAsync(
            reader,
            writer,
            sourcePath,
            root.ResourceId,
            cancellationToken);
        DateTime modifiedAtUtc = FogWriteTimestamp.Resolve(
            requestedModifiedAtUtc,
            latestModifiedAt);
        FogResourceElementFactory.Create(
            request,
            root.ResourceId,
            modifiedAtUtc).WriteTo(writer);
        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
        await output.FlushAsync(cancellationToken);
        output.Flush(flushToDisk: true);

        return new FogRewriteOutcome(
            root.ResourceId,
            root.NextCounter,
            modifiedAtUtc);
    }
}
