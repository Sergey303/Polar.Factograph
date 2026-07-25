using System.Text;
using System.Xml;

namespace Polar.Factograph.Fog;

internal static class FogXmlReaderFactory
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

    static FogXmlReaderFactory()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static XmlReader Create(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return XmlReader.Create(stream, Settings);
    }
}
