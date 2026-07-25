using System.Runtime.CompilerServices;

namespace Polar.Factograph.Fog;

internal sealed class FogCurrentRecordReader
{
    public async IAsyncEnumerable<FogCurrentRecord> ReadAsync(
        FogMaterializationPlan plan,
        FogRecordStreamFactory openRecords,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(openRecords);

        if (!plan.ContainsCurrentCassetteRoot)
        {
            yield return FogCurrentRecordFactory.CreateSyntheticCassetteRoot();
        }

        await foreach (FogSourceRecord record in openRecords(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (plan.Includes(record))
            {
                yield return FogCurrentRecordFactory.Create(record, plan);
            }
        }
    }
}
