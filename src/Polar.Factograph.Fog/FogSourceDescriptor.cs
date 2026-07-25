using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed record FogSourceDescriptor(
    string CassetteId,
    string CassetteName,
    string FogPath,
    string? DatabaseId,
    string? CassetteUri,
    string? Owner,
    string? Prefix,
    long? Counter,
    bool Writable,
    bool IsCassetteMetadata,
    long Length,
    DateTime LastWriteTimeUtc);

public interface IFogSourceScanner
{
    Task<IReadOnlyList<FogSourceDescriptor>> ScanAsync(
        ProjectDefinition project,
        CancellationToken cancellationToken = default);
}
