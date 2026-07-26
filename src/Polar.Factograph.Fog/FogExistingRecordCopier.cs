using System.Xml;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

internal static class FogExistingRecordCopier
{
    public static async Task CopyAsync(
        XmlReader reader,
        XmlWriter writer,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        if (reader.IsEmptyElement)
        {
            return;
        }

        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == 0)
            {
                return;
            }

            if (reader.NodeType != XmlNodeType.Element || reader.Depth != 1)
            {
                continue;
            }

            using XmlReader subtree = reader.ReadSubtree();
            if (!await subtree.ReadAsync())
            {
                throw new InvalidDataException($"Fog record is empty: {sourcePath}");
            }

            XElement element = await XElement.LoadAsync(
                subtree,
                LoadOptions.None,
                cancellationToken);
            element.WriteTo(writer);
        }

        throw new InvalidDataException($"Fog root element is not closed: {sourcePath}");
    }
}
