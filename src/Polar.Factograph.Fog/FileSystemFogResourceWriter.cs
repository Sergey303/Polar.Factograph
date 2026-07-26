namespace Polar.Factograph.Fog;

public sealed class FileSystemFogResourceWriter : IFogResourceWriter
{
    private readonly TimeProvider _timeProvider;

    public FileSystemFogResourceWriter(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<FogResourceWriteResult> AppendAsync(
        FogSourceDescriptor source,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(source, request);
        string targetPath = Path.GetFullPath(source.FogPath);
        string temporaryPath = CreateTemporaryPath(targetPath);
        DateTime modifiedAtUtc = TruncateToSeconds(
            _timeProvider.GetUtcNow().UtcDateTime);

        await using FogWriteLease lease = FogWriteLease.Acquire(targetPath);
        try
        {
            FogRewriteOutcome outcome = await FogResourceFileRewriter.RewriteAsync(
                targetPath,
                temporaryPath,
                request,
                modifiedAtUtc,
                cancellationToken);
            await FogWrittenFileValidator.ValidateAsync(
                temporaryPath,
                source,
                outcome.ResourceId,
                outcome.NextCounter,
                cancellationToken);
            FogAtomicFileCommitter.Commit(temporaryPath, targetPath);

            return new FogResourceWriteResult(
                outcome.ResourceId,
                targetPath,
                outcome.NextCounter,
                modifiedAtUtc);
        }
        catch
        {
            FogAtomicFileCommitter.DeleteTemporary(temporaryPath);
            throw;
        }
    }

    private static void Validate(
        FogSourceDescriptor source,
        FogResourceWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(request);
        if (!source.Writable || source.Prefix is null || source.Counter is null)
        {
            throw new InvalidOperationException(
                $"Fog source is not writable: {source.FogPath}");
        }

        if (!File.Exists(source.FogPath))
        {
            throw new FileNotFoundException(
                $"Fog file was not found: {source.FogPath}",
                source.FogPath);
        }
    }

    private static string CreateTemporaryPath(string targetPath) =>
        Path.Combine(
            Path.GetDirectoryName(targetPath)!,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
}
