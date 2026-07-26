namespace Polar.Factograph.Fog;

public sealed class FileSystemFogDirectiveWriter : IFogDirectiveWriter
{
    private readonly TimeProvider _timeProvider;

    public FileSystemFogDirectiveWriter(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<FogDirectiveWriteResult> AppendAsync(
        FogSourceDescriptor source,
        FogDirectiveWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        FogDirectiveWriteContext context = FogDirectiveWritePreparation.Prepare(
            source,
            request,
            _timeProvider);
        await using FogWriteLease lease = FogWriteLease.Acquire(context.TargetPath);

        try
        {
            FogDirectiveRewriteOutcome outcome = await FogDirectiveFileRewriter.RewriteAsync(
                context.TargetPath,
                context.TemporaryPath,
                request,
                context.ModifiedAtUtc,
                cancellationToken);
            await FogDirectiveWrittenFileValidator.ValidateAsync(
                context.TemporaryPath,
                source,
                request,
                outcome.Counter,
                outcome.ModifiedAtUtc,
                cancellationToken);
            FogAtomicFileCommitter.Commit(context.TemporaryPath, context.TargetPath);

            return new FogDirectiveWriteResult(
                request.Kind,
                FogIdentifier.Clean(request.ResourceId),
                request.SubstituteTargetId is null
                    ? null
                    : FogIdentifier.Clean(request.SubstituteTargetId),
                context.TargetPath,
                outcome.ModifiedAtUtc);
        }
        catch
        {
            FogAtomicFileCommitter.DeleteTemporary(context.TemporaryPath);
            throw;
        }
    }
}
