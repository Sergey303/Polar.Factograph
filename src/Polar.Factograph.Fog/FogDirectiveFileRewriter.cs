using System.Xml;

namespace Polar.Factograph.Fog;

internal sealed record FogDirectiveRewriteOutcome(
    long Counter,
    DateTime ModifiedAtUtc);

internal static class FogDirectiveFileRewriter
{
    public static async Task<FogDirectiveRewriteOutcome> RewriteAsync(
        string sourcePath,
        string temporaryPath,
        FogDirectiveWriteRequest request,
        DateTime requestedModifiedAtUtc,
        CancellationToken cancellationToken)
    {
        await using FileStream input = FogRewriteStreamFactory.OpenInput(sourcePath);
        using XmlReader reader = FogXmlReaderFactory.Create(input);
        await using FileStream output = FogRewriteStreamFactory.OpenOutput(temporaryPath);
        using XmlWriter writer = FogRewriteStreamFactory.CreateWriter(output);

        await writer.WriteStartDocumentAsync();
        if (await reader.MoveToContentAsync() != XmlNodeType.Element)
        {
            throw new InvalidDataException($"Fog file is empty: {sourcePath}");
        }

        FogDirectiveRootState root = FogDirectiveRootState.Read(reader, sourcePath);
        await FogXmlRootWriter.WriteStartAsync(reader, writer, root.WrittenCounter);
        DateTime? latestResourceRevision = await FogExistingRecordCopier.CopyAsync(
            reader,
            writer,
            sourcePath,
            FogIdentifier.Clean(request.ResourceId),
            cancellationToken);
        DateTime modifiedAtUtc = FogWriteTimestamp.Resolve(
            requestedModifiedAtUtc,
            latestResourceRevision);
        FogDirectiveElementFactory.Create(request, modifiedAtUtc).WriteTo(writer);
        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
        await output.FlushAsync(cancellationToken);
        output.Flush(flushToDisk: true);

        return new FogDirectiveRewriteOutcome(root.Counter, modifiedAtUtc);
    }
}
