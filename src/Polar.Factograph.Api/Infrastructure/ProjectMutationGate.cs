namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectMutationGate
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);

    public async Task<ProjectMutationLease> AcquireAsync(
        string indexRoot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        string fullRoot = Path.GetFullPath(indexRoot);
        Directory.CreateDirectory(fullRoot);
        string lockPath = Path.Combine(fullRoot, ".mutation.lock");

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                FileStream stream = new(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
                return new ProjectMutationLease(stream);
            }
            catch (IOException)
            {
                await Task.Delay(RetryDelay, cancellationToken);
            }
        }
    }
}
