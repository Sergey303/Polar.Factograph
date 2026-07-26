using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

public sealed record CassettePreviewQueueStatus(
    string CassetteId,
    string CassetteName,
    int Queued,
    int Processing,
    int Failed,
    DateTimeOffset? OldestQueuedAtUtc);

public sealed record ProjectPreviewQueueStatus(
    int Queued,
    int Processing,
    int Failed,
    IReadOnlyList<CassettePreviewQueueStatus> Cassettes);

public sealed class CassettePreviewQueueStatusReader
{
    public ProjectPreviewQueueStatus Read(ProjectDefinition project)
    {
        ArgumentNullException.ThrowIfNull(project);
        CassettePreviewQueueStatus[] cassettes = project.Cassettes
            .Where(cassette => cassette.Enabled)
            .Select(ReadCassette)
            .OrderBy(status => status.CassetteId, StringComparer.Ordinal)
            .ToArray();
        return new ProjectPreviewQueueStatus(
            cassettes.Sum(status => status.Queued),
            cassettes.Sum(status => status.Processing),
            cassettes.Sum(status => status.Failed),
            cassettes);
    }

    private static CassettePreviewQueueStatus ReadCassette(CassetteDefinition cassette)
    {
        CassettePreviewQueuePaths paths = CassettePreviewQueuePaths.Create(cassette);
        string[] queued = Enumerate(paths.Queued);
        return new CassettePreviewQueueStatus(
            cassette.Id,
            cassette.Name,
            queued.Length,
            Enumerate(paths.Processing).Length,
            Enumerate(paths.Failed).Length,
            queued.Length == 0
                ? null
                : queued.Min(path => new DateTimeOffset(File.GetLastWriteTimeUtc(path))));
    }

    private static string[] Enumerate(string directory) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();
}