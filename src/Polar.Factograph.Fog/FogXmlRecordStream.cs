using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogXmlRecordStream
{
    public static async IAsyncEnumerable<XElement> ReadAsync(
        string fogPath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using FileStream stream = new(
            fogPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        using XmlReader reader = FogXmlReaderFactory.Create(stream);

        while (await ReadNextAsync(reader, fogPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.Element && reader.Depth == 1)
            {
                yield return await ReadElementAsync(reader, fogPath, cancellationToken);
            }
        }
    }

    private static async Task<bool> ReadNextAsync(XmlReader reader, string fogPath)
    {
        try
        {
            return await reader.ReadAsync();
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException($"Fog XML cannot be read: {fogPath}", exception);
        }
    }

    private static async Task<XElement> ReadElementAsync(
        XmlReader reader,
        string fogPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using XmlReader subtree = reader.ReadSubtree();
            if (!await subtree.ReadAsync())
            {
                throw new InvalidDataException($"Fog record is empty: {fogPath}");
            }

            return await XElement.LoadAsync(subtree, LoadOptions.None, cancellationToken);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException($"Fog record cannot be read: {fogPath}", exception);
        }
    }
}
