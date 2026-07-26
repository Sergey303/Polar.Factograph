using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Tests;

internal sealed class StubFogSourceScanner(
    IReadOnlyList<FogSourceDescriptor> sources) : IFogSourceScanner
{
    public int CallCount { get; private set; }

    public Task<IReadOnlyList<FogSourceDescriptor>> ScanAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(sources);
    }
}
