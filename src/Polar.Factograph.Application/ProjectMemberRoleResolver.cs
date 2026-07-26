using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectMemberRoleResolver
{
    public static MemberDefinition? FindMember(ProjectDefinition project, string userId) =>
        project.Members.FirstOrDefault(candidate =>
            string.Equals(candidate.UserId, userId, StringComparison.Ordinal));

    public static RoleDefinition[] ResolveRoles(
        ProjectDefinition project,
        MemberDefinition member) =>
        member.Roles
            .Select(roleName => project.Roles.TryGetValue(roleName, out RoleDefinition? role)
                ? role
                : throw new InvalidDataException(
                    $"Unknown role '{roleName}' for user '{member.UserId}'."))
            .ToArray();

    public static HashSet<string> CollectProjectRights(
        IEnumerable<RoleDefinition> roles) =>
        roles.SelectMany(role => role.ProjectRights)
            .ToHashSet(StringComparer.Ordinal);
}
