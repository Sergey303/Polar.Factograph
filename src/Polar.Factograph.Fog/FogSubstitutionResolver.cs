namespace Polar.Factograph.Fog;

internal static class FogSubstitutionResolver
{
    public static IReadOnlyDictionary<string, string?> Resolve(
        IReadOnlyDictionary<string, string?> substitutions)
    {
        ArgumentNullException.ThrowIfNull(substitutions);
        Dictionary<string, string?> result = new(StringComparer.Ordinal);

        foreach ((string sourceId, string? directTarget) in substitutions)
        {
            result[sourceId] = directTarget is null
                ? null
                : ResolveTarget(sourceId, directTarget, substitutions);
        }

        return result;
    }

    private static string ResolveTarget(
        string sourceId,
        string directTarget,
        IReadOnlyDictionary<string, string?> substitutions)
    {
        HashSet<string> visited = new(StringComparer.Ordinal) { sourceId };
        string current = directTarget;

        while (substitutions.TryGetValue(current, out string? next))
        {
            if (!visited.Add(current))
            {
                string chain = string.Join(" -> ", visited.Append(current));
                throw new InvalidDataException($"Cyclic Fog substitute chain: {chain}.");
            }

            // Legacy behavior stops at a deleted target. References point to that id,
            // while the deleted resource itself remains absent from current records.
            if (next is null)
            {
                return current;
            }

            current = next;
        }

        return current;
    }
}
