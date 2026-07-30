using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Documents;

public sealed record DocumentLocationResponse(
    string? CassetteId,
    string? CassetteName,
    bool OriginalAvailable,
    bool IconPreviewAvailable,
    bool SmallPreviewAvailable,
    bool MediumPreviewAvailable,
    bool NormalPreviewAvailable);

public static class DocumentLocationPresentation
{
    public static DocumentLocationResponse Present(
        CassetteDocumentLocation location,
        ProjectAccessSnapshot access)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(access);

        bool exposeCassette = access.HasProjectRight(ProjectRights.RebuildIndex) ||
            CanReplace(location.CassetteId, access);
        return new DocumentLocationResponse(
            exposeCassette ? location.CassetteId : null,
            exposeCassette ? location.CassetteName : null,
            location.OriginalPath is not null,
            location.IconPreviewPath is not null,
            location.SmallPreviewPath is not null,
            location.MediumPreviewPath is not null,
            location.NormalPreviewPath is not null);
    }

    private static bool CanReplace(
        string cassetteId,
        ProjectAccessSnapshot access) =>
        access.Cassettes.TryGetValue(cassetteId, out CassetteAccessSnapshot? snapshot) &&
        snapshot.Enabled &&
        snapshot.AllowWrite &&
        snapshot.Has(CassetteRights.ReplaceDocuments);
}
