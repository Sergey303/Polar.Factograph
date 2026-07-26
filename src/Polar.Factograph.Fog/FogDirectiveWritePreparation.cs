namespace Polar.Factograph.Fog;

internal sealed record FogDirectiveWriteContext(
    string TargetPath,
    string TemporaryPath,
    DateTime ModifiedAtUtc);

internal static class FogDirectiveWritePreparation
{
    public static FogDirectiveWriteContext Prepare(
        FogSourceDescriptor source,
        FogDirectiveWriteRequest request,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ValidateRequest(request);
        if (!source.Writable)
        {
            throw new InvalidOperationException($"Fog source is not writable: {source.FogPath}");
        }

        string targetPath = Path.GetFullPath(source.FogPath);
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException($"Fog file was not found: {targetPath}", targetPath);
        }

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        return new FogDirectiveWriteContext(
            targetPath,
            Path.Combine(Path.GetDirectoryName(targetPath)!, $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp"),
            new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc));
    }

    private static void ValidateRequest(FogDirectiveWriteRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ResourceId);
        if (request.Kind == FogRecordKind.Delete && request.SubstituteTargetId is not null)
        {
            throw new ArgumentException("Delete directive cannot have a substitute target.");
        }

        if (request.Kind != FogRecordKind.Delete && request.Kind != FogRecordKind.Substitute)
        {
            throw new ArgumentException($"Unsupported Fog directive kind: {request.Kind}.");
        }

        if (request.Kind == FogRecordKind.Substitute)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.SubstituteTargetId);
            if (string.Equals(FogIdentifier.Clean(request.ResourceId), FogIdentifier.Clean(request.SubstituteTargetId), StringComparison.Ordinal))
            {
                throw new ArgumentException("Substitute source and target identifiers must differ.");
            }
        }
    }
}
