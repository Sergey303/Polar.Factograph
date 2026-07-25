using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectWriteRoutingValidator
{
    public static void Validate(ProjectDefinition project)
    {
        foreach ((string roleName, string cassetteId) in project.WriteRouting.DefaultCassetteByRole)
        {
            RoleDefinition role = RequireRole(project, roleName);
            CassetteDefinition cassette = RequireCassette(project, roleName, cassetteId);

            if (!cassette.Enabled || !cassette.AllowWrite)
            {
                throw new InvalidDataException(
                    $"Write routing for role '{roleName}' targets non-writable cassette '{cassetteId}'.");
            }

            if (!HasWriteRight(role, cassetteId))
            {
                throw new InvalidDataException(
                    $"Role '{roleName}' has no write right for routed cassette '{cassetteId}'.");
            }
        }
    }

    private static RoleDefinition RequireRole(ProjectDefinition project, string roleName) =>
        project.Roles.TryGetValue(roleName, out RoleDefinition? role)
            ? role
            : throw new InvalidDataException(
                $"Write routing references unknown role '{roleName}'.");

    private static CassetteDefinition RequireCassette(
        ProjectDefinition project,
        string roleName,
        string cassetteId) =>
        project.Cassettes.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, cassetteId, StringComparison.Ordinal))
        ?? throw new InvalidDataException(
            $"Write routing for role '{roleName}' references unknown cassette '{cassetteId}'.");

    private static bool HasWriteRight(RoleDefinition role, string cassetteId)
    {
        IEnumerable<string> rights = Enumerable.Empty<string>();
        if (role.CassetteRights.TryGetValue(
                ProjectConfigurationRules.Wildcard,
                out string[]? wildcard))
        {
            rights = rights.Concat(wildcard);
        }

        if (role.CassetteRights.TryGetValue(cassetteId, out string[]? exact))
        {
            rights = rights.Concat(exact);
        }

        return rights.Any(ProjectConfigurationRules.WriteRights.Contains);
    }
}
