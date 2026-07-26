namespace Polar.Factograph.Fog;

public sealed record FogDirectiveWriteRequest(
    FogRecordKind Kind,
    string ResourceId,
    string? SubstituteTargetId = null);

public sealed record FogDirectiveWriteResult(
    FogRecordKind Kind,
    string ResourceId,
    string? SubstituteTargetId,
    string FogPath,
    DateTime ModifiedAtUtc);

public interface IFogDirectiveWriter
{
    Task<FogDirectiveWriteResult> AppendAsync(
        FogSourceDescriptor source,
        FogDirectiveWriteRequest request,
        CancellationToken cancellationToken = default);
}
