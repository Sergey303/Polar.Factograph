using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectCassetteAccessBuilder
{
    public static IReadOnlyDictionary<string, CassetteAccessSnapshot> Build(
        ProjectDefinition project,
        MemberDefinition member,
        IReadOnlyList<RoleDefinition> roles,
        IReadOnlySet<string> projectRights)
    {
        Dictionary<string, CassetteAccessSnapshot> result = new(StringComparer.Ordinal);

        foreach (CassetteDefinition cassette in project.Cassettes)
        {
            HashSet<string> rights = ProjectCassetteRightsComposer.Compose(
                cassette,
                member,
                roles,
                projectRights);
            result.Add(cassette.Id, new CassetteAccessSnapshot(
                cassette.Id,
                cassette.Enabled,
                cassette.AllowWrite,
                rights));
        }

        return result;
    }
}
