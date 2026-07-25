using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed record CassetteAccessSnapshot(
    string CassetteId,
    bool Enabled,
    bool AllowWrite,
    IReadOnlySet<string> Rights)
{
    public bool CanRead => Rights.Contains(CassetteRights.Read);

    public bool Has(string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return Rights.Contains(right);
    }
}

public sealed record ProjectAccessSnapshot(
    string UserId,
    bool IsMember,
    IReadOnlySet<string> ProjectRights,
    IReadOnlyDictionary<string, CassetteAccessSnapshot> Cassettes,
    string? DefaultWriteCassetteId)
{
    public bool HasProjectRight(string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return ProjectRights.Contains(right);
    }

    public IReadOnlySet<string> ReadableCassetteIds => Cassettes.Values
        .Where(cassette => cassette.Enabled && cassette.CanRead)
        .Select(cassette => cassette.CassetteId)
        .ToHashSet(StringComparer.Ordinal);

    public bool HasCassetteRight(string cassetteId, string right)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(right);
        return Cassettes.TryGetValue(cassetteId, out CassetteAccessSnapshot? cassette) &&
               cassette.Enabled &&
               cassette.Has(right);
    }
}

/// <summary>
/// Calculates one immutable access snapshot from project roles, cassette defaults, and member overrides.
/// </summary>
public sealed class ProjectAccessService
{
    private const string Wildcard = "*";

    private static readonly HashSet<string> WriteRights = new(StringComparer.Ordinal)
    {
        CassetteRights.WriteMetadata,
        CassetteRights.AddDocuments,
        CassetteRights.ReplaceDocuments,
        CassetteRights.Delete,
        CassetteRights.Substitute,
        CassetteRights.Manage
    };

    public ProjectAccessSnapshot Evaluate(ProjectDefinition project, string userId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        MemberDefinition? member = project.Members.FirstOrDefault(candidate =>
            string.Equals(candidate.UserId, userId, StringComparison.Ordinal));
        if (member is null)
        {
            return new ProjectAccessSnapshot(
                userId,
                IsMember: false,
                new HashSet<string>(StringComparer.Ordinal),
                new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal),
                DefaultWriteCassetteId: null);
        }

        RoleDefinition[] roles = member.Roles
            .Select(roleName => project.Roles.TryGetValue(roleName, out RoleDefinition? role)
                ? role
                : throw new InvalidDataException($"Unknown role '{roleName}' for user '{member.UserId}'."))
            .ToArray();

        HashSet<string> projectRights = roles
            .SelectMany(role => role.ProjectRights)
            .ToHashSet(StringComparer.Ordinal);
        Dictionary<string, CassetteAccessSnapshot> cassetteAccess = new(StringComparer.Ordinal);

        foreach (CassetteDefinition cassette in project.Cassettes)
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
                AddConfiguredRights(role.CassetteRights, Wildcard, rights);
                AddConfiguredRights(role.CassetteRights, cassette.Id, rights);
            }

            ApplyMemberOverrides(member.CassetteOverrides, cassette.Id, rights);

            if (!cassette.Enabled)
            {
                rights.Clear();
            }
            else if (!cassette.AllowWrite)
            {
                rights.ExceptWith(WriteRights);
            }

            cassetteAccess.Add(
                cassette.Id,
                new CassetteAccessSnapshot(
                    cassette.Id,
                    cassette.Enabled,
                    cassette.AllowWrite,
                    rights));
        }

        string? defaultWriteCassetteId = ResolveDefaultWriteCassette(
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

    private static void AddConfiguredRights(
        IReadOnlyDictionary<string, string[]> configured,
        string cassetteId,
        ISet<string> target)
    {
        if (!configured.TryGetValue(cassetteId, out string[]? rights))
        {
            return;
        }

        foreach (string right in rights)
        {
            if (!string.IsNullOrWhiteSpace(right))
            {
                target.Add(right);
            }
        }
    }

    private static void ApplyMemberOverrides(
        IReadOnlyDictionary<string, string[]> overrides,
        string cassetteId,
        HashSet<string> rights)
    {
        string[]? replacement = null;
        if (overrides.TryGetValue(Wildcard, out string[]? wildcard))
        {
            replacement = wildcard;
        }

        if (overrides.TryGetValue(cassetteId, out string[]? exact))
        {
            replacement = exact;
        }

        if (replacement is null)
        {
            return;
        }

        rights.Clear();
        foreach (string right in replacement)
        {
            if (!string.IsNullOrWhiteSpace(right))
            {
                rights.Add(right);
            }
        }
    }

    private static string? ResolveDefaultWriteCassette(
        ProjectDefinition project,
        MemberDefinition member,
        IReadOnlyDictionary<string, CassetteAccessSnapshot> cassetteAccess)
    {
        foreach (string roleName in member.Roles)
        {
            if (!project.WriteRouting.DefaultCassetteByRole.TryGetValue(
                    roleName,
                    out string? cassetteId) ||
                !cassetteAccess.TryGetValue(cassetteId, out CassetteAccessSnapshot? access) ||
                !access.Enabled ||
                !access.AllowWrite ||
                !access.Rights.Overlaps(WriteRights))
            {
                continue;
            }

            return cassetteId;
        }

        return null;
    }
}