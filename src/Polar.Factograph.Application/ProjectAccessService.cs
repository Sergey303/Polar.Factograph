using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

/// <summary>
/// Calculates one immutable access snapshot from project roles, cassette defaults, and member overrides.
/// </summary>
public sealed class ProjectAccessService
{
    public ProjectAccessSnapshot Evaluate(ProjectDefinition project, string userId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        MemberDefinition? member = ProjectMemberRoleResolver.FindMember(project, userId);
        if (member is null)
        {
            return CreateNonMemberSnapshot(userId);
        }

        RoleDefinition[] roles = ProjectMemberRoleResolver.ResolveRoles(project, member);
        HashSet<string> projectRights =
            ProjectMemberRoleResolver.CollectProjectRights(roles);
        IReadOnlyDictionary<string, CassetteAccessSnapshot> cassetteAccess =
            ProjectCassetteAccessBuilder.Build(project, member, roles, projectRights);
        string? defaultWriteCassetteId = ProjectDefaultWriteCassetteResolver.Resolve(
            project,
            member,
            cassetteAccess);

        return new ProjectAccessSnapshot(
            member.UserId,
            IsMember: true,
            projectRights,
            cassetteAccess,
            defaultWriteCassetteId);
    }

    private static ProjectAccessSnapshot CreateNonMemberSnapshot(string userId) => new(
        userId,
        IsMember: false,
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal),
        DefaultWriteCassetteId: null);
}
