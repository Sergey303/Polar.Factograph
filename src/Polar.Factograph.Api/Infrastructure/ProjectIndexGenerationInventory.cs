namespace Polar.Factograph.Api.Infrastructure;

internal sealed record ProjectIndexGenerationInventory(
    int CompletedCount,
    int BuildingCount);

internal static class ProjectIndexGenerationInventoryReader
{
    private const string BuildingSuffix = ".building";

    public static ProjectIndexGenerationInventory Read(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        string root = Path.GetFullPath(indexRoot);
        if (!Directory.Exists(root))
        {
            return new ProjectIndexGenerationInventory(0, 0);
        }

        int completed = 0;
        int building = 0;
        foreach (string directory in Directory.EnumerateDirectories(root))
        {
            string name = Path.GetFileName(directory);
            if (name.EndsWith(BuildingSuffix, StringComparison.Ordinal))
            {
                string baseName = name[..^BuildingSuffix.Length];
                building += ProjectIndexPointerReader.TryParseGenerationId(baseName, out _) ? 1 : 0;
            }
            else
            {
                completed += ProjectIndexPointerReader.TryParseGenerationId(name, out _) ? 1 : 0;
            }
        }

        return new ProjectIndexGenerationInventory(completed, building);
    }
}
