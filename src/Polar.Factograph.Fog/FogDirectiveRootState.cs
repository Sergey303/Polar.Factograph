using System.Globalization;
using System.Xml;

namespace Polar.Factograph.Fog;

internal sealed record FogDirectiveRootState(
    long Counter,
    string WrittenCounter)
{
    public static FogDirectiveRootState Read(XmlReader reader, string fogPath)
    {
        if (reader.NodeType != XmlNodeType.Element ||
            reader.Depth != 0 ||
            !string.Equals(reader.LocalName, "RDF", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Fog root element must be rdf:RDF: {fogPath}");
        }

        string counterText = reader.GetAttribute("counter")
            ?? throw new InvalidDataException($"Writable Fog has no counter: {fogPath}");
        if (!long.TryParse(counterText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long counter))
        {
            throw new InvalidDataException(
                $"Fog counter is not an integer in '{fogPath}': {counterText}");
        }

        return new FogDirectiveRootState(counter, counterText);
    }
}
