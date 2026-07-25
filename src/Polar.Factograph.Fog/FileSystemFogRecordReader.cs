using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Polar.Factograph.Fog;

public sealed class FileSystemFogRecordReader : IFogRecordReader
{
    public async IAsyncEnumerable<FogSourceRecord> ReadAsync(
        FogSourceDescriptor source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        long sourceOrdinal = 0;

        await foreach (XElement element in FogXmlRecordStream.ReadAsync(
                           source.FogPath,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            yield return LegacyFogCanonicalizer.Canonicalize(
                source,
                sourceOrdinal++,
                element);
        }
    }
}
