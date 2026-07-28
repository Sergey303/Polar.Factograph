using System.Globalization;

namespace Polar.Factograph.Fog;

internal static class FogDirectiveWrittenFileValidator
{
    public static async Task ValidateAsync(
        string temporaryPath,
        FogSourceDescriptor source,
        FogDirectiveWriteRequest request,
        long expectedCounter,
        DateTime expectedModifiedAtUtc,
        CancellationToken cancellationToken)
    {
        FogRootMetadata metadata = await new FogRootMetadataReader()
            .ReadAsync(temporaryPath, cancellationToken);
        if (metadata.Counter != expectedCounter)
        {
            throw new InvalidDataException($"Rewritten Fog counter is invalid: {temporaryPath}");
        }

        FileInfo file = new(temporaryPath);
        FogSourceDescriptor temporarySource = source with
        {
            FogPath = file.FullName,
            Length = file.Length,
            LastWriteTimeUtc = file.LastWriteTimeUtc
        };
        string resourceId = FogIdentifier.Clean(request.ResourceId);
        string? targetId = request.SubstituteTargetId is null
            ? null
            : FogIdentifier.Clean(request.SubstituteTargetId);
        string expectedModifiedAtRaw = expectedModifiedAtUtc
            .ToUniversalTime()
            .ToString("u", CultureInfo.InvariantCulture);

        await foreach (FogSourceRecord record in new FileSystemFogRecordReader()
                           .ReadAsync(temporarySource, cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (Matches(
                    record,
                    request.Kind,
                    resourceId,
                    targetId,
                    expectedModifiedAtRaw))
            {
                return;
            }
        }

        throw new InvalidDataException(
            $"Rewritten Fog does not contain the expected directive: {temporaryPath}");
    }

    private static bool Matches(
        FogSourceRecord record,
        FogRecordKind kind,
        string resourceId,
        string? targetId,
        string modifiedAtRaw) =>
        record.Kind == kind &&
        string.Equals(record.ResourceId, resourceId, StringComparison.Ordinal) &&
        string.Equals(record.SubstituteTargetId, targetId, StringComparison.Ordinal) &&
        string.Equals(record.ModifiedAtRaw, modifiedAtRaw, StringComparison.Ordinal);
}
