namespace Polar.Factograph.Fog;

public sealed record FogResourceWriteRequest(
    string TypeId,
    IReadOnlyList<FogProperty> Properties,
    string? ResourceId = null);

public sealed record FogResourceWriteResult(
    string ResourceId,
    string FogPath,
    long NextCounter,
    DateTime ModifiedAtUtc);

public interface IFogResourceWriter
{
    Task<FogResourceWriteResult> AppendAsync(
        FogSourceDescriptor source,
        FogResourceWriteRequest request,
        CancellationToken cancellationToken = default);
}
