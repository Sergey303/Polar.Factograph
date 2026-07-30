using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public enum ResourceProvenanceDetail
{
    None = 0,
    Cassette = 1,
    Full = 2
}

public sealed record PresentedResourceProvenance(
    string SourceCassetteId,
    Guid? SourceRecordId = null,
    string? SourceFogPath = null,
    DateTimeOffset? ModifiedAt = null);

public static class ResourceProvenancePresentation
{
    public static ResourceProvenanceDetail Resolve(
        ProjectResourcePortrait portrait,
        ProjectAccessSnapshot access)
    {
        ArgumentNullException.ThrowIfNull(portrait);
        ArgumentNullException.ThrowIfNull(access);

        if (access.HasProjectRight(ProjectRights.RebuildIndex))
        {
            return ResourceProvenanceDetail.Full;
        }

        return access.HasCassetteRight(
            portrait.Provenance.SourceCassetteId,
            CassetteRights.WriteMetadata)
            ? ResourceProvenanceDetail.Cassette
            : ResourceProvenanceDetail.None;
    }

    public static PresentedResourceProvenance? Present(
        ResourceProvenance provenance,
        ResourceProvenanceDetail detail)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        return detail switch
        {
            ResourceProvenanceDetail.None => null,
            ResourceProvenanceDetail.Cassette => new PresentedResourceProvenance(
                provenance.SourceCassetteId),
            ResourceProvenanceDetail.Full => new PresentedResourceProvenance(
                provenance.SourceCassetteId,
                provenance.SourceRecordId,
                provenance.SourceFogPath,
                provenance.ModifiedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(detail), detail, null)
        };
    }
}
