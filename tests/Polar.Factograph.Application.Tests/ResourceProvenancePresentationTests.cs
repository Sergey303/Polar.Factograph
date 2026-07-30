using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ResourceProvenancePresentationTests
{
    private static readonly ResourceProvenance Provenance = new(
        Guid.Parse("10000000-0000-0000-0000-000000000001"),
        "cassette-a",
        "meta/current.fog",
        new DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Resolve_HidesProvenanceFromReadOnlyViewer()
    {
        ProjectResourcePortrait portrait = Portrait();
        ProjectAccessSnapshot access = Access(
            projectRights: [ProjectRights.Read],
            cassetteRights: [CassetteRights.Read]);

        ResourceProvenanceDetail detail = ResourceProvenancePresentation.Resolve(
            portrait,
            access);
        PresentedResourceProvenance? presented = ResourceProvenancePresentation.Present(
            portrait.Provenance,
            detail);

        Assert.Equal(ResourceProvenanceDetail.None, detail);
        Assert.Null(presented);
    }

    [Fact]
    public void Resolve_ExposesOnlySourceCassetteToMetadataEditor()
    {
        ProjectResourcePortrait portrait = Portrait();
        ProjectAccessSnapshot access = Access(
            projectRights: [ProjectRights.Read],
            cassetteRights: [CassetteRights.Read, CassetteRights.WriteMetadata]);

        ResourceProvenanceDetail detail = ResourceProvenancePresentation.Resolve(
            portrait,
            access);
        PresentedResourceProvenance? presented = ResourceProvenancePresentation.Present(
            portrait.Provenance,
            detail);

        Assert.Equal(ResourceProvenanceDetail.Cassette, detail);
        Assert.NotNull(presented);
        Assert.Equal("cassette-a", presented.SourceCassetteId);
        Assert.Null(presented.SourceRecordId);
        Assert.Null(presented.SourceFogPath);
        Assert.Null(presented.ModifiedAt);
    }

    [Fact]
    public void Resolve_ExposesFullProvenanceToIndexAdministrator()
    {
        ProjectResourcePortrait portrait = Portrait();
        ProjectAccessSnapshot access = Access(
            projectRights: [ProjectRights.Read, ProjectRights.RebuildIndex],
            cassetteRights: [CassetteRights.Read]);

        ResourceProvenanceDetail detail = ResourceProvenancePresentation.Resolve(
            portrait,
            access);
        PresentedResourceProvenance? presented = ResourceProvenancePresentation.Present(
            portrait.Provenance,
            detail);

        Assert.Equal(ResourceProvenanceDetail.Full, detail);
        Assert.NotNull(presented);
        Assert.Equal(Provenance.SourceCassetteId, presented.SourceCassetteId);
        Assert.Equal(Provenance.SourceRecordId, presented.SourceRecordId);
        Assert.Equal(Provenance.SourceFogPath, presented.SourceFogPath);
        Assert.Equal(Provenance.ModifiedAt, presented.ModifiedAt);
    }

    [Fact]
    public void Resolve_DoesNotExposeCassetteFromDifferentWritableCassette()
    {
        ProjectResourcePortrait portrait = Portrait();
        ProjectAccessSnapshot access = new(
            "editor",
            IsMember: true,
            new HashSet<string>([ProjectRights.Read], StringComparer.Ordinal),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
            {
                ["cassette-a"] = new CassetteAccessSnapshot(
                    "cassette-a",
                    Enabled: true,
                    AllowWrite: false,
                    new HashSet<string>([CassetteRights.Read], StringComparer.Ordinal)),
                ["cassette-b"] = new CassetteAccessSnapshot(
                    "cassette-b",
                    Enabled: true,
                    AllowWrite: true,
                    new HashSet<string>(
                        [CassetteRights.Read, CassetteRights.WriteMetadata],
                        StringComparer.Ordinal))
            },
            DefaultWriteCassetteId: "cassette-b");

        ResourceProvenanceDetail detail = ResourceProvenancePresentation.Resolve(
            portrait,
            access);

        Assert.Equal(ResourceProvenanceDetail.None, detail);
    }

    private static ProjectResourcePortrait Portrait() => new(
        "resource-1",
        Type: null,
        Literals: [],
        DirectLinks: [],
        InverseLinks: [],
        Provenance);

    private static ProjectAccessSnapshot Access(
        IReadOnlySet<string> projectRights,
        IReadOnlySet<string> cassetteRights) => new(
        "user-1",
        IsMember: true,
        projectRights,
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["cassette-a"] = new CassetteAccessSnapshot(
                "cassette-a",
                Enabled: true,
                AllowWrite: cassetteRights.Contains(CassetteRights.WriteMetadata),
                cassetteRights)
        },
        DefaultWriteCassetteId: null);
}
