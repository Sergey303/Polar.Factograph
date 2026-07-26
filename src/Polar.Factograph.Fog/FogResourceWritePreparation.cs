namespace Polar.Factograph.Fog;

internal sealed record FogResourceWriteContext(
    string TargetPath,
    string TemporaryPath,
    DateTime ModifiedAtUtc);

internal static class FogResourceWritePreparation
{
    public static FogResourceWriteContext Prepare(
        FogSourceDescriptor source,
        FogResourceWriteRequest request,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (!source.Writable || source.Prefix is null || source.Counter is null)
        {
            throw new InvalidOperationException(
                $"Fog source is not writable: {source.FogPath}");
        }

        string targetPath = Path.GetFullPath(source.FogPath);
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException(
                $"Fog file was not found: {targetPath}",
                targetPath);
        }

        string temporaryPath = Path.Combine(
            Path.GetDirectoryName(targetPath)!,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        DateTime modifiedAtUtc = new(
            now.Ticks - now.Ticks % TimeSpan.TicksPerSecond,
            DateTimeKind.Utc);
        return new FogResourceWriteContext(
            targetPath,
            temporaryPath,
            modifiedAtUtc);
    }
}
