using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Authentication;

public sealed class IdentityProjectMemberOverlay(
    IdentityJsonStore store,
    LocalAuthenticationOptions options)
{
    public ProjectDefinition Apply(ProjectDefinition project, string userId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (options.IsPublicUser(userId))
        {
            return WithCurrentMember(project, userId, [LocalAuthenticationOptions.PublicViewerRole]);
        }

        IdentityUser? user = store.FindUser(userId);
        return user is { Enabled: true }
            ? WithCurrentMember(project, user.Id, user.Roles)
            : project;
    }

    private static ProjectDefinition WithCurrentMember(
        ProjectDefinition project,
        string userId,
        string[] roles)
    {
        MemberDefinition current = new()
        {
            UserId = userId,
            Roles = roles,
            CassetteOverrides = new Dictionary<string, string[]>(StringComparer.Ordinal)
        };

        return project with
        {
            Members =
            [
                .. project.Members.Where(member => !string.Equals(
                    member.UserId,
                    userId,
                    StringComparison.Ordinal)),
                current
            ]
        };
    }
}
