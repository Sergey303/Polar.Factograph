using System.Globalization;
using System.Xml;

namespace Polar.Factograph.Fog;

internal sealed record FogWriteRootState(
    string ResourceId,
    long NextCounter)
{
    public static FogWriteRootState Read(
        XmlReader reader,
        FogResourceWriteRequest request,
        string fogPath)
    {
        if (reader.NodeType != XmlNodeType.Element ||
            reader.Depth != 0 ||
            !string.Equals(reader.LocalName, "RDF", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Fog root element must be rdf:RDF: {fogPath}");
        }

        string prefix = reader.GetAttribute("prefix")
            ?? throw new InvalidDataException($"Writable Fog has no prefix: {fogPath}");
        string counterText = reader.GetAttribute("counter")
            ?? throw new InvalidDataException($"Writable Fog has no counter: {fogPath}");
        if (!long.TryParse(
                counterText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long counter))
        {
            throw new InvalidDataException(
                $"Fog counter is not an integer in '{fogPath}': {counterText}");
        }

        if (!string.IsNullOrWhiteSpace(request.ResourceId))
        {
            return new FogWriteRootState(
                FogIdentifier.Clean(request.ResourceId),
                counter);
        }

        return new FogWriteRootState(
            FogIdentifier.Clean(prefix + counter.ToString(CultureInfo.InvariantCulture)),
            checked(counter + 1));
    }
}
