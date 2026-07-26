using System.Text;

namespace Polar.Factograph.Api.Infrastructure;

internal static class ProjectIndexPointerReader
{
    private const string CurrentFileName = "CURRENT";
    private const string GenerationPrefix = "generation-";

    public static ProjectIndexPointerSnapshot Read(string indexRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexRoot);
        string root = Path.GetFullPath(indexRoot);
        string currentPath = Path.Combine(root, CurrentFileName);
        if (!File.Exists(currentPath))
        {
            return new ProjectIndexPointerSnapshot("missing", null, false);
        }

        string name = File.ReadAllText(currentPath, Encoding.UTF8).Trim();
        if (!TryParseGenerationId(name, out Guid generationId))
        {
            return new ProjectIndexPointerSnapshot("invalid", null, false);
        }

        return new ProjectIndexPointerSnapshot(
            "valid",
            generationId,
            Directory.Exists(Path.Combine(root, name)));
    }

    public static bool TryParseGenerationId(string name, out Guid generationId)
    {
        generationId = default;
        return name.StartsWith(GenerationPrefix, StringComparison.Ordinal) &&
               name.Length == GenerationPrefix.Length + 32 &&
               Guid.TryParseExact(name[GenerationPrefix.Length..], "N", out generationId);
    }
}
