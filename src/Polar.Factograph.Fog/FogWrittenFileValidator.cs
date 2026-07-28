using System.Globalization;

namespace Polar.Factograph.Fog;

internal static class FogWrittenFileValidator
{
    public static async Task ValidateAsync(
        string temporaryPath,
        FogSourceDescriptor source,
        string resourceId,
        long expectedCounter,
        DateTime expectedModifiedAtUtc,
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
        string expectedModifiedAtRaw = expectedModifiedAtUtc
            .ToUniversalTime()
            .ToString("u", CultureInfo.InvariantCulture);

        bool found = false;
        await foreach (FogSourceRecord record in new FileSystemFogRecordReader()
                           .ReadAsync(temporarySource, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (record.Kind == FogRecordKind.Resource &&
                string.Equals(record.ResourceId, resourceId, StringComparison.Ordinal) &&
                string.Equals(
                    record.ModifiedAtRaw,
                    expectedModifiedAtRaw,
                    StringComparison.Ordinal))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            throw new InvalidDataException(
                $"Rewritten Fog does not contain the new revision of '{resourceId}': {temporaryPath}");
        }
    }
}
