using System.Text.Json;
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
            OldestRequestedAt(queued));
    }

    private static DateTimeOffset? OldestRequestedAt(IEnumerable<string> paths)
    {
        DateTimeOffset[] timestamps = paths
            .Select(ReadRequestedAt)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        return timestamps.Length == 0 ? null : timestamps.Min();
    }

    private static DateTimeOffset? ReadRequestedAt(string path)
    {
        try
        {
            using FileStream stream = File.OpenRead(path);
            using JsonDocument document = JsonDocument.Parse(stream);
            return document.RootElement.TryGetProperty("requestedAtUtc", out JsonElement value) &&
                   value.TryGetDateTimeOffset(out DateTimeOffset timestamp)
                ? timestamp
                : null;
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private static string[] Enumerate(string directory) =>
        Directory.Exists(directory)
            ? Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            : Array.Empty<string>();
}