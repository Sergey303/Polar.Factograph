using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectBuiltInAccess
{
    public const string ViewerRole = "viewer";
    public const string EditorRole = "editor";
    public const string AdministratorRole = "administrator";

    public static ProjectDefinition Apply(ProjectDefinition project)
    {
        ArgumentNullException.ThrowIfNull(project);

        CassetteDefinition writeCassette = project.Cassettes.SingleOrDefault(cassette =>
                cassette.Enabled && cassette.AllowWrite)
            ?? throw new InvalidDataException(
                "Project configuration must contain exactly one writable cassette.");

        return project with
        {
            Roles = CreateRoles(writeCassette.Id),
            Members = Array.Empty<MemberDefinition>()
        };
    }

    private static Dictionary<string, RoleDefinition> CreateRoles(string writeCassetteId) =>
        new(StringComparer.Ordinal)
        {
            [ViewerRole] = new RoleDefinition
            {
                ProjectRights = [ProjectRights.Read, ProjectRights.Search]
            },
            [EditorRole] = new RoleDefinition
            {
                ProjectRights = [ProjectRights.Read, ProjectRights.Search],
                CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [writeCassetteId] =
                    [
                        CassetteRights.Read,
                        CassetteRights.WriteMetadata,
                        CassetteRights.AddDocuments,
                        CassetteRights.ReplaceDocuments
                    ]
                }
            },
            [AdministratorRole] = new RoleDefinition
            {
                ProjectRights =
                [
                    ProjectRights.Read,
                    ProjectRights.Search,
                    ProjectRights.Export,
                    ProjectRights.ManageUsers,
                    ProjectRights.ManageCassettes,
                    ProjectRights.RebuildIndex
                ],
                CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    [ProjectConfigurationRules.Wildcard] =
                    [
                        CassetteRights.Read,
                        CassetteRights.WriteMetadata,
                        CassetteRights.AddDocuments,
                        CassetteRights.ReplaceDocuments,
                        CassetteRights.Delete,
                        CassetteRights.Substitute,
                        CassetteRights.Manage
                    ]
                }
            }
        };
}
