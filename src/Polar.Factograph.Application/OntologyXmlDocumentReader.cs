using System.Xml;
using System.Xml.Linq;

namespace Polar.Factograph.Application;

internal static class OntologyXmlDocumentReader
{
    private static readonly XmlReaderSettings Settings = new()
    {
        Async = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true
    };

    public static async Task<XDocument> ReadAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = new(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using XmlReader reader = XmlReader.Create(stream, Settings);
            return await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException(
                $"Ontology XML cannot be read: {fullPath}",
                exception);
        }
    }
}
