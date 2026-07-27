using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Authentication;

public sealed class IdentityProjectMemberOverlay(IdentityJsonStore store)
{
    public ProjectDefinition Apply(ProjectDefinition project, string userId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        if (project.Members.Any(member => string.Equals(
                member.UserId,
                userId,
                StringComparison.Ordinal)))
        {
            return project;
        }

        IdentityUser? user = store.FindUser(userId);
        if (user is null || !user.Enabled)
        {
            return project;
        }

        return project with
        {
            Members =
            [
                .. project.Members,
                new MemberDefinition
                {
                    UserId = user.Id,
                    Roles = user.Roles,
                    CassetteOverrides = new Dictionary<string, string[]>(StringComparer.Ordinal)
                }
            ]
        };
    }
}
