using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class LegacyFogCanonicalizer
{
    private static readonly XNamespace Rdf = LegacyFogVocabulary.RdfNamespace;

    public static FogSourceRecord? Canonicalize(
        FogSourceDescriptor source,
        long sourceOrdinal,
        XElement element)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(element);

        string localName = element.Name.LocalName;
        string? modifiedAtRaw = element.Attribute("mT")?.Value;
        DateTime modifiedAt = LegacyFogTime.Parse(modifiedAtRaw, source.FogPath);

        if (string.Equals(localName, "delete", StringComparison.Ordinal))
        {
            string? targetId = element.Attribute(Rdf + "about")?.Value
                ?? element.Attribute("id")?.Value;
            if (string.IsNullOrWhiteSpace(targetId))
            {
                // Some legacy user Fog files contain an empty <delete/> marker. It has
                // no target and therefore cannot change the materialized RDF cloud.
                return null;
            }

            return FogDirectiveCanonicalizer.Delete(
                source,
                sourceOrdinal,
                element,
                modifiedAt,
                modifiedAtRaw);
        }

        return string.Equals(localName, "substitute", StringComparison.Ordinal)
            ? FogDirectiveCanonicalizer.Substitute(
                source,
                sourceOrdinal,
                element,
                modifiedAt,
                modifiedAtRaw)
            : FogResourceCanonicalizer.Canonicalize(
                source,
                sourceOrdinal,
                element,
                modifiedAt,
                modifiedAtRaw);
    }
}
