using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Tests;

internal sealed class RecordingFogResourceWriter(
    FogResourceWriteResult result) : IFogResourceWriter
{
    public int CallCount { get; private set; }
    public FogSourceDescriptor? Source { get; private set; }
    public FogResourceWriteRequest? Request { get; private set; }

    public Task<FogResourceWriteResult> AppendAsync(
        FogSourceDescriptor source,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        Source = source;
        Request = request;
        return Task.FromResult(result);
    }
}
