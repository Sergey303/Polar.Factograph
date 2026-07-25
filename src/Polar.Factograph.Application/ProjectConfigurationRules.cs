using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectConfigurationRules
{
    public const string NoDefaultAccess = "none";
    public const string Wildcard = "*";

    public static IReadOnlySet<string> KnownProjectRights { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            ProjectRights.Read,
            ProjectRights.Search,
            ProjectRights.Export,
            ProjectRights.ManageUsers,
            ProjectRights.ManageCassettes,
            ProjectRights.RebuildIndex
        };

    public static IReadOnlySet<string> KnownCassetteRights { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            CassetteRights.Read,
            CassetteRights.WriteMetadata,
            CassetteRights.AddDocuments,
            CassetteRights.ReplaceDocuments,
            CassetteRights.Delete,
            CassetteRights.Substitute,
            CassetteRights.Manage
        };

    public static IReadOnlySet<string> WriteRights { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            CassetteRights.WriteMetadata,
            CassetteRights.AddDocuments,
            CassetteRights.ReplaceDocuments,
            CassetteRights.Delete,
            CassetteRights.Substitute,
            CassetteRights.Manage
        };
}
