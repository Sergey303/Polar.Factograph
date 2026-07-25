using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectAuthorizationException : UnauthorizedAccessException
{
    public ProjectAuthorizationException(string userId, string requiredRight)
        : base($"User '{userId}' does not have required project right '{requiredRight}'.")
    {
        UserId = userId;
        RequiredRight = requiredRight;
    }

    public string UserId { get; }

    public string RequiredRight { get; }
}

public static class ProjectAuthorization
{
    public static IReadOnlySet<string> RequireRead(ProjectAccessSnapshot access) =>
        Require(access, ProjectRights.Read);

    public static IReadOnlySet<string> RequireSearch(ProjectAccessSnapshot access)
    {
        _ = Require(access, ProjectRights.Read);
        return Require(access, ProjectRights.Search);
    }

    private static IReadOnlySet<string> Require(
        ProjectAccessSnapshot access,
        string projectRight)
    {
        ArgumentNullException.ThrowIfNull(access);
        if (!access.IsMember || !access.HasProjectRight(projectRight))
        {
            throw new ProjectAuthorizationException(access.UserId, projectRight);
        }

        return access.ReadableCassetteIds;
    }
}