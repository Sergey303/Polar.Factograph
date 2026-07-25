using System.Runtime.CompilerServices;

namespace Polar.Factograph.Fog;

public sealed class FogProjectRecordSource(IFogRecordReader reader)
{
    public async IAsyncEnumerable<FogSourceRecord> ReadAsync(
        IReadOnlyList<FogSourceDescriptor> sources,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach (FogSourceDescriptor source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await foreach (FogSourceRecord record in reader
                               .ReadAsync(source, cancellationToken)
                               .WithCancellation(cancellationToken))
            {
                yield return record;
            }
        }
    }
}
