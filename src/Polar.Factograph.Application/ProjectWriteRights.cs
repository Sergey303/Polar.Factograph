using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectWriteRights
{
    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            CassetteRights.WriteMetadata,
            CassetteRights.AddDocuments,
            CassetteRights.ReplaceDocuments,
            CassetteRights.Delete,
            CassetteRights.Substitute,
            CassetteRights.Manage
        };

    public static bool HasAny(IReadOnlySet<string> rights) =>
        rights.Overlaps(All);
}
