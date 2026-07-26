namespace Polar.Factograph.Fog;

internal static class FogWrittenFileValidator
{
    public static async Task ValidateAsync(
        string temporaryPath,
        FogSourceDescriptor source,
        string resourceId,
        long expectedCounter,
        CancellationToken cancellationToken)
    {
        FogRootMetadata metadata = await new FogRootMetadataReader()
            .ReadAsync(temporaryPath, cancellationToken);
        if (metadata.Counter != expectedCounter)
        {
            throw new InvalidDataException(
                $"Rewritten Fog counter is invalid: {temporaryPath}");
        }

        FileInfo file = new(temporaryPath);
        FogSourceDescriptor temporarySource = source with
        {
            FogPath = file.FullName,
            Length = file.Length,
            LastWriteTimeUtc = file.LastWriteTimeUtc
        };

        FogSourceRecord? written = null;
        await foreach (FogSourceRecord record in new FileSystemFogRecordReader()
                           .ReadAsync(temporarySource, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (record.Kind == FogRecordKind.Resource &&
                string.Equals(record.ResourceId, resourceId, StringComparison.Ordinal))
            {
                written = record;
            }
        }

        if (written is null)
        {
            throw new InvalidDataException(
                $"Rewritten Fog does not contain resource '{resourceId}': {temporaryPath}");
        }
    }
}
