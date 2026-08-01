using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Endpoints;

public sealed record ProjectCassetteOverview(
    string Id,
    string Name,
    bool AllowWrite,
    IReadOnlyList<string> Rights);

public sealed record ProjectOverview(
    string ProjectId,
    string Name,
    bool CanAdmin,
    IReadOnlyList<ProjectCassetteOverview> Cassettes,
    string? DefaultWriteCassetteId)
{
    public string? HomeResourceId { get; init; }
}

public static class ProjectOverviewPresentation
{
    private static readonly IReadOnlySet<string> WriteRights = new HashSet<string>(
        [
            CassetteRights.WriteMetadata,
            CassetteRights.AddDocuments,
            CassetteRights.ReplaceDocuments,
            CassetteRights.Delete,
            CassetteRights.Substitute,
            CassetteRights.Manage
        ],
        StringComparer.Ordinal);

    public static ProjectOverview Present(
        ProjectDefinition project,
        ProjectAccessSnapshot access)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(access);

        bool canAdmin = access.HasProjectRight(ProjectRights.RebuildIndex);
        ProjectCassetteOverview[] cassettes = project.Cassettes
            .Where(cassette => ShouldExpose(cassette, access, canAdmin))
            .OrderBy(cassette => cassette.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(cassette => cassette.Id, StringComparer.Ordinal)
            .Select(cassette => PresentCassette(cassette, access, canAdmin))
            .ToArray();
        HashSet<string> exposedIds = cassettes
            .Select(cassette => cassette.Id)
            .ToHashSet(StringComparer.Ordinal);
        string? defaultWriteCassetteId = access.DefaultWriteCassetteId is { } defaultId &&
            exposedIds.Contains(defaultId)
            ? defaultId
            : null;
        string? homeResourceId = string.IsNullOrWhiteSpace(project.HomeResourceId)
            ? null
            : project.HomeResourceId.Trim();

        return new ProjectOverview(
            project.ProjectId,
            project.Name,
            canAdmin,
            cassettes,
            defaultWriteCassetteId)
        {
            HomeResourceId = homeResourceId
        };
    }

    private static bool ShouldExpose(
        CassetteDefinition cassette,
        ProjectAccessSnapshot access,
        bool canAdmin)
    {
        if (!cassette.Enabled ||
            !access.Cassettes.TryGetValue(cassette.Id, out CassetteAccessSnapshot? snapshot) ||
            !snapshot.Enabled ||
            !snapshot.CanRead)
        {
            return false;
        }

        return canAdmin ||
            EffectiveAllowWrite(cassette, snapshot) &&
            snapshot.Rights.Any(WriteRights.Contains);
    }

    private static ProjectCassetteOverview PresentCassette(
        CassetteDefinition cassette,
        ProjectAccessSnapshot access,
        bool canAdmin)
    {
        CassetteAccessSnapshot snapshot = access.Cassettes[cassette.Id];
        string[] rights = snapshot.Rights
            .Where(right => canAdmin || WriteRights.Contains(right))
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new ProjectCassetteOverview(
            cassette.Id,
            cassette.Name,
            EffectiveAllowWrite(cassette, snapshot),
            rights);
    }

    private static bool EffectiveAllowWrite(
        CassetteDefinition cassette,
        CassetteAccessSnapshot snapshot) =>
        cassette.AllowWrite && snapshot.AllowWrite;
}
