using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectCassetteAccessBuilder
{
    private const string Wildcard = "*";

    public static IReadOnlyDictionary<string, CassetteAccessSnapshot> Build(
        ProjectDefinition project,
        MemberDefinition member,
        IReadOnlyList<RoleDefinition> roles,
        IReadOnlySet<string> projectRights)
    {
        Dictionary<string, CassetteAccessSnapshot> result = new(StringComparer.Ordinal);

        foreach (CassetteDefinition cassette in project.Cassettes)
        {
            HashSet<string> rights = BuildRights(cassette, member, roles, projectRights);
            result.Add(cassette.Id, new CassetteAccessSnapshot(
                cassette.Id,
                cassette.Enabled,
                cassette.AllowWrite,
                rights));
        }

        return result;
    }

    private static HashSet<string> BuildRights(
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
        foreach (string right in rights.Where(right => !string.IsNullOrWhiteSpace(right)))
        {
            target.Add(right);
        }
    }

    private static void ApplyOverride(
        IReadOnlyDictionary<string, string[]> overrides,
        string cassetteId,
        HashSet<string> rights)
    {
        string[]? replacement = overrides.GetValueOrDefault(Wildcard);
        replacement = overrides.GetValueOrDefault(cassetteId) ?? replacement;
        if (replacement is null) return;

        rights.Clear();
        rights.UnionWith(replacement.Where(right => !string.IsNullOrWhiteSpace(right)));
    }
}
