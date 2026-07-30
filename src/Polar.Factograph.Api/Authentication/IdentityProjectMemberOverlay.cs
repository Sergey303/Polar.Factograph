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
            return ApplyPublicViewer(project, userId);
        }

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

    private static ProjectDefinition ApplyPublicViewer(
        ProjectDefinition project,
        string userId)
    {
        if (!project.Roles.ContainsKey(LocalAuthenticationOptions.PublicViewerRole))
        {
            throw new InvalidOperationException(
                $"Public reading requires project role '{LocalAuthenticationOptions.PublicViewerRole}'.");
        }

        MemberDefinition member = new()
        {
            UserId = userId,
            Roles = [LocalAuthenticationOptions.PublicViewerRole],
            CassetteOverrides = new Dictionary<string, string[]>(StringComparer.Ordinal)
        };
        return project with
        {
            Members =
            [
                .. project.Members.Where(existing => !string.Equals(
                    existing.UserId,
                    userId,
                    StringComparison.Ordinal)),
                member
            ]
        };
    }
}
