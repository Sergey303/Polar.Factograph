using System.Xml;

namespace Polar.Factograph.Fog;

internal static class FogWriteXmlName
{
    public static string LocalName(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        string localName = id.StartsWith(
            LegacyFogVocabulary.Namespace,
            StringComparison.Ordinal)
            ? id[LegacyFogVocabulary.Namespace.Length..]
            : id;

        try
        {
            XmlConvert.VerifyNCName(localName);
            return localName;
        }
        catch (XmlException exception)
        {
            throw new ArgumentException(
                $"Fog term is not a valid XML local name: {id}",
                nameof(id),
                exception);
        }
    }
}
