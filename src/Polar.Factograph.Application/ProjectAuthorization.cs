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
    public static IReadOnlySet<string> RequireRead(ProjectAccessSnapshot access)
    {
        RequireProjectRight(access, ProjectRights.Read);
        return access.ReadableCassetteIds;
    }

    public static IReadOnlySet<string> RequireSearch(ProjectAccessSnapshot access)
    {
        RequireProjectRight(access, ProjectRights.Read);
        RequireProjectRight(access, ProjectRights.Search);
        return access.ReadableCassetteIds;
    }

    public static IReadOnlySet<string> RequireWritableCassetteRight(
        ProjectAccessSnapshot access,
        string cassetteRight)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteRight);
        RequireProjectRight(access, ProjectRights.Read);

        HashSet<string> allowed = access.Cassettes.Values
            .Where(snapshot =>
                snapshot.Enabled &&
                snapshot.AllowWrite &&
                snapshot.CanRead &&
                snapshot.Rights.Contains(cassetteRight))
            .Select(snapshot => snapshot.CassetteId)
            .ToHashSet(StringComparer.Ordinal);
        if (allowed.Count == 0)
        {
            throw new ProjectAuthorizationException(access.UserId, cassetteRight);
        }

        return allowed;
    }

    public static void RequireProjectRight(
        ProjectAccessSnapshot access,
        string projectRight)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRight);

        if (!access.IsMember || !access.HasProjectRight(projectRight))
        {
            throw new ProjectAuthorizationException(access.UserId, projectRight);
        }
    }
}
