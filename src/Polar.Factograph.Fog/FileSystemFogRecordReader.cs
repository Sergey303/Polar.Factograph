using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

public sealed class FileSystemFogRecordReader : IFogRecordReader
{
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        Async = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreWhitespace = true,
        CloseInput = true
    };

    static FileSystemFogRecordReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async IAsyncEnumerable<FogSourceRecord> ReadAsync(
        FogSourceDescriptor source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        FileStream stream = new(
            source.FogPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using XmlReader reader = XmlReader.Create(stream, ReaderSettings);
        long sourceOrdinal = 0;

        while (await ReadAsync(reader, source.FogPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1)
            {
                continue;
            }

            XElement element = await ReadElementAsync(reader, source.FogPath, cancellationToken);
            yield return LegacyFogCanonicalizer.Canonicalize(source, sourceOrdinal++, element);
        }
    }

    private static async Task<bool> ReadAsync(XmlReader reader, string fogPath)
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

internal static class LegacyFogCanonicalizer
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;
    private static readonly XNamespace Xml = LegacyFogVocabulary.XmlNamespace;

    public static FogSourceRecord Canonicalize(
        FogSourceDescriptor source,
        long sourceOrdinal,
        XElement element)
    {
        string localName = element.Name.LocalName;
        DateTime modifiedAt = LegacyFogTime.Parse(element.Attribute("mT")?.Value, source.FogPath);
        string? modifiedAtRaw = element.Attribute("mT")?.Value;

        if (string.Equals(localName, "delete", StringComparison.Ordinal))
        {
            string resourceId = RequiredId(
                element.Attribute(Rdf + "about")?.Value ?? element.Attribute("id")?.Value,
                source.FogPath,
                "delete");

            return new FogSourceRecord(
                new FogRecordKey(source.FogPath, sourceOrdinal),
                source.CassetteId,
                source.CassetteName,
                FogRecordKind.Delete,
                resourceId,
                null,
                null,
                modifiedAt,
                modifiedAtRaw,
                Array.Empty<FogProperty>());
        }

        if (string.Equals(localName, "substitute", StringComparison.Ordinal))
        {
            string oldId = RequiredId(
                element.Attribute("old-id")?.Value ?? element.Attribute(Rdf + "about")?.Value,
                source.FogPath,
                "substitute old-id");

            string? newIdValue = element.Attribute("new-id")?.Value;
            if (string.IsNullOrWhiteSpace(newIdValue))
            {
                newIdValue = element.Elements()
                    .FirstOrDefault(child => string.Equals(child.Name.LocalName, "newid", StringComparison.Ordinal))
                    ?.Attribute(Rdf + "resource")
                    ?.Value;
            }

            string newId = RequiredId(newIdValue, source.FogPath, "substitute new-id");

            return new FogSourceRecord(
                new FogRecordKey(source.FogPath, sourceOrdinal),
                source.CassetteId,
                source.CassetteName,
                FogRecordKind.Substitute,
                oldId,
                null,
                newId,
                modifiedAt,
                modifiedAtRaw,
                Array.Empty<FogProperty>());
        }

        string id = RequiredId(element.Attribute(Rdf + "about")?.Value, source.FogPath, localName);
        List<FogProperty> properties = new();

        foreach (XElement child in element.Elements())
        {
            if (string.Equals(child.Name.LocalName, "iisstore", StringComparison.Ordinal))
            {
                string? uri = child.Attribute("uri")?.Value;
                if (!string.IsNullOrWhiteSpace(uri))
                {
                    properties.Add(new FogProperty(
                        LegacyFogVocabulary.Namespace + "uri",
                        FogPropertyKind.Literal,
                        uri));
                }

                continue;
            }

            string predicate = LegacyFogVocabulary.Namespace + child.Name.LocalName;
            string? resource = child.Attribute(Rdf + "resource")?.Value;
            if (resource is not null)
            {
                properties.Add(new FogProperty(
                    predicate,
                    FogPropertyKind.Resource,
                    CleanId(resource)));
                continue;
            }

            properties.Add(new FogProperty(
                predicate,
                FogPropertyKind.Literal,
                child.Value,
                child.Attribute(Xml + "lang")?.Value,
                child.Attribute(Rdf + "datatype")?.Value));
        }

        return new FogSourceRecord(
            new FogRecordKey(source.FogPath, sourceOrdinal),
            source.CassetteId,
            source.CassetteName,
            FogRecordKind.Resource,
            id,
            LegacyFogVocabulary.Namespace + localName,
            null,
            modifiedAt,
            modifiedAtRaw,
            properties);
    }

    private static string RequiredId(string? value, string fogPath, string recordDescription)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(
                $"Fog {recordDescription} record has no identifier: {fogPath}");
        }

        return CleanId(value);
    }

    private static string CleanId(string value) => value.Replace("|", string.Empty, StringComparison.Ordinal);
}

internal static class LegacyFogTime
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static DateTime Parse(string? value, string fogPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return DateTime.MinValue;
        }

        DateTimeStyles styles = DateTimeStyles.AllowWhiteSpaces;
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, styles, out DateTime invariant) ||
            DateTime.TryParse(value, RussianCulture, styles, out invariant) ||
            DateTime.TryParse(value, CultureInfo.CurrentCulture, styles, out invariant))
        {
            return invariant;
        }

        throw new InvalidDataException($"Fog mT value cannot be parsed in '{fogPath}': {value}");
    }
}
