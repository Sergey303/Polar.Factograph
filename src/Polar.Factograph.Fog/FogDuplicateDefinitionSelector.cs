namespace Polar.Factograph.Fog;

internal static class FogDuplicateDefinitionSelector
{
    public static async Task<IReadOnlyDictionary<string, FogRecordKey>> SelectAsync(
        IReadOnlySet<string> duplicateIds,
        FogRecordStreamFactory openRecords,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(duplicateIds);
        ArgumentNullException.ThrowIfNull(openRecords);

        Dictionary<string, DefinitionWinner> winners = new(StringComparer.Ordinal);
        if (duplicateIds.Count == 0)
        {
            return new Dictionary<string, FogRecordKey>(StringComparer.Ordinal);
        }

        long sequence = 0;
        await foreach (FogSourceRecord record in openRecords(cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            long currentSequence = sequence++;
            if (record.Kind != FogRecordKind.Resource || !duplicateIds.Contains(record.ResourceId))
            {
                continue;
            }

            if (!winners.TryGetValue(record.ResourceId, out DefinitionWinner winner) ||
                record.ModifiedAt > winner.ModifiedAt ||
                record.ModifiedAt == winner.ModifiedAt && currentSequence > winner.Sequence)
            {
                winners[record.ResourceId] = new DefinitionWinner(
                    record.ModifiedAt,
                    currentSequence,
                    record.Key);
            }
        }

        return winners.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Key,
            StringComparer.Ordinal);
    }

    private readonly record struct DefinitionWinner(
        DateTime ModifiedAt,
        long Sequence,
        FogRecordKey Key);
}
