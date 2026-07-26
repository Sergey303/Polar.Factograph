using System.Text;
using System.Xml;

namespace Polar.Factograph.Fog;

internal static class FogRewriteStreamFactory
{
    private static readonly XmlWriterSettings WriterSettings = new()
    {
        Async = true,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = true,
        CloseOutput = false
    };

    public static FileStream OpenInput(string path) => new(
        path,
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read,
        bufferSize: 64 * 1024,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    public static FileStream OpenOutput(string path) => new(
        path,
        FileMode.CreateNew,
        FileAccess.Write,
        FileShare.None,
        bufferSize: 64 * 1024,
        FileOptions.Asynchronous | FileOptions.SequentialScan);

    public static XmlWriter CreateWriter(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        return XmlWriter.Create(output, WriterSettings);
    }
}
