using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed record FogSourceDescriptor(
    string CassetteId,
    string CassetteName,
    string FogPath,
    string? Owner,
    string? Prefix,
    long? Counter,
    bool Writable,
    bool IsCassetteMetadata);

public interface IFogSourceScanner
{
    Task<IReadOnlyList<FogSourceDescriptor>> ScanAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default);
}
