namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectMutationLease : IAsyncDisposable
{
    private readonly FileStream _stream;

    internal ProjectMutationLease(FileStream stream)
    {
        _stream = stream;
    }

    public ValueTask DisposeAsync() => _stream.DisposeAsync();
}
