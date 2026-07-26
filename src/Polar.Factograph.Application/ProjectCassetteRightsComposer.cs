using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectCassetteRightsComposer
{
    private const string Wildcard = "*";

    public static HashSet<string> Compose(
        CassetteDefinition cassette,
        MemberDefinition member,
        IReadOnlyList<RoleDefinition> roles,
        IReadOnlySet<string> projectRights)
    {
        HashSet<string> rights = new(StringComparer.Ordinal);
        if (cassette.Enabled &&
            string.Equals(cassette.DefaultAccess, ProjectRights.Read, StringComparison.Ordinal) &&
            projectRights.Contains(ProjectRights.Read))
        {
            rights.Add(CassetteRights.Read);
        }

        foreach (RoleDefinition role in roles)
        {
            AddConfigured(role.CassetteRights, Wildcard, rights);
            AddConfigured(role.CassetteRights, cassette.Id, rights);
        }

        ApplyOverride(member.CassetteOverrides, cassette.Id, rights);
        if (!cassette.Enabled)
        {
            rights.Clear();
        }
        else if (!cassette.AllowWrite)
        {
            rights.ExceptWith(ProjectWriteRights.All);
        }

        return rights;
    }

    private static void AddConfigured(
        IReadOnlyDictionary<string, string[]> configured,
        string cassetteId,
        ISet<string> target)
    {
        if (!configured.TryGetValue(cassetteId, out string[]? rights)) return;
        target.UnionWith(rights.Where(right => !string.IsNullOrWhiteSpace(right)));
    }

    private static void ApplyOverride(
        IReadOnlyDictionary<string, string[]> overrides,
        string cassetteId,
        HashSet<string> rights)
    {
        string[]? replacement = overrides.GetValueOrDefault(cassetteId)
            ?? overrides.GetValueOrDefault(Wildcard);
        if (replacement is null) return;

        rights.Clear();
        rights.UnionWith(replacement.Where(right => !string.IsNullOrWhiteSpace(right)));
    }
}
