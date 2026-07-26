using System.Xml;

namespace Polar.Factograph.Fog;

internal static class FogXmlRootWriter
{
    public static async Task WriteStartAsync(
        XmlReader reader,
        XmlWriter writer,
        string writtenCounter)
    {
        await writer.WriteStartElementAsync(
            reader.Prefix,
            reader.LocalName,
            reader.NamespaceURI);

        if (!reader.HasAttributes)
        {
            return;
        }

        bool hasAttribute = reader.MoveToFirstAttribute();
        while (hasAttribute)
        {
            string value = reader.NamespaceURI.Length == 0 &&
                           string.Equals(reader.LocalName, "counter", StringComparison.Ordinal)
                ? writtenCounter
                : reader.Value;
            await writer.WriteAttributeStringAsync(
                reader.Prefix,
                reader.LocalName,
                reader.NamespaceURI,
                value);
            hasAttribute = reader.MoveToNextAttribute();
        }

        reader.MoveToElement();
    }
}
