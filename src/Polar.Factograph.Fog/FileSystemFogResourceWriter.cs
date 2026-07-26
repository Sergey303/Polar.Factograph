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
        FogResourceWriteContext context = FogResourceWritePreparation.Prepare(
            source,
            request,
            _timeProvider);
        await using FogWriteLease lease = FogWriteLease.Acquire(context.TargetPath);

        try
        {
            FogRewriteOutcome outcome = await FogResourceFileRewriter.RewriteAsync(
                context.TargetPath,
                context.TemporaryPath,
                request,
                context.ModifiedAtUtc,
                cancellationToken);
            await FogWrittenFileValidator.ValidateAsync(
                context.TemporaryPath,
                source,
                outcome.ResourceId,
                outcome.NextCounter,
                cancellationToken);
            FogAtomicFileCommitter.Commit(
                context.TemporaryPath,
                context.TargetPath);

            return new FogResourceWriteResult(
                outcome.ResourceId,
                context.TargetPath,
                outcome.NextCounter,
                context.ModifiedAtUtc);
        }
        catch
        {
            FogAtomicFileCommitter.DeleteTemporary(context.TemporaryPath);
            throw;
        }
    }
}
