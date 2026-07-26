using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog;

internal sealed record CassettePreviewQueuePaths(
    string Queued,
    string Processing,
    string Failed)
{
    public static CassettePreviewQueuePaths Create(CassetteDefinition cassette)
    {
        ArgumentNullException.ThrowIfNull(cassette);
        string documents = Path.Combine(Path.GetFullPath(cassette.Path), "documents");
        return new CassettePreviewQueuePaths(
            Path.Combine(documents, "preview-queue"),
            Path.Combine(documents, "preview-processing"),
            Path.Combine(documents, "preview-failed"));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(Queued);
        Directory.CreateDirectory(Processing);
        Directory.CreateDirectory(Failed);
    }
}