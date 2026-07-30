using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectAuthorizationTests
{
    [Fact]
    public void RequireRead_RejectsUnknownUser()
    {
        ProjectAccessSnapshot access = Snapshot(
            isMember: false,
            projectRights: Array.Empty<string>(),
            readableCassette: false);

        ProjectAuthorizationException exception = Assert.Throws<ProjectAuthorizationException>(
            () => ProjectAuthorization.RequireRead(access));

        Assert.Equal("user", exception.UserId);
        Assert.Equal(ProjectRights.Read, exception.RequiredRight);
    }

    [Fact]
    public void RequireSearch_RequiresBothReadAndSearchRights()
    {
        ProjectAccessSnapshot access = Snapshot(
            isMember: true,
            projectRights: [ProjectRights.Read],
            readableCassette: true);

        ProjectAuthorizationException exception = Assert.Throws<ProjectAuthorizationException>(
            () => ProjectAuthorization.RequireSearch(access));

        Assert.Equal(ProjectRights.Search, exception.RequiredRight);
    }

    [Fact]
    public void RequireSearch_ReturnsOnlyReadableEnabledCassettes()
    {
        Dictionary<string, CassetteAccessSnapshot> cassettes = new(StringComparer.Ordinal)
        {
            ["readable"] = new(
                "readable",
                Enabled: true,
                AllowWrite: false,
                new HashSet<string>(StringComparer.Ordinal) { CassetteRights.Read }),
            ["write-only"] = new(
                "write-only",
                Enabled: true,
                AllowWrite: true,
                new HashSet<string>(StringComparer.Ordinal) { CassetteRights.WriteMetadata }),
            ["disabled"] = new(
                "disabled",
                Enabled: false,
                AllowWrite: false,
                new HashSet<string>(StringComparer.Ordinal) { CassetteRights.Read })
        };
        ProjectAccessSnapshot access = new(
            "user",
            IsMember: true,
            new HashSet<string>(StringComparer.Ordinal)
            {
                ProjectRights.Read,
                ProjectRights.Search
            },
            cassettes,
            DefaultWriteCassetteId: null);

        IReadOnlySet<string> allowed = ProjectAuthorization.RequireSearch(access);

        Assert.Equal(new[] { "readable" }, allowed);
    }

    [Fact]
    public void RequireWritableCassetteRight_ReturnsOnlyEffectiveReadableWrites()
    {
        ProjectAccessSnapshot access = new(
            "user",
            IsMember: true,
            Rights(ProjectRights.Read),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
            {
                ["allowed"] = new(
                    "allowed",
                    Enabled: true,
                    AllowWrite: true,
                    Rights(CassetteRights.Read, CassetteRights.WriteMetadata)),
                ["read-only"] = new(
                    "read-only",
                    Enabled: true,
                    AllowWrite: false,
                    Rights(CassetteRights.Read, CassetteRights.WriteMetadata)),
                ["write-only"] = new(
                    "write-only",
                    Enabled: true,
                    AllowWrite: true,
                    Rights(CassetteRights.WriteMetadata)),
                ["disabled"] = new(
                    "disabled",
                    Enabled: false,
                    AllowWrite: true,
                    Rights(CassetteRights.Read, CassetteRights.WriteMetadata))
            },
            DefaultWriteCassetteId: "allowed");

        IReadOnlySet<string> allowed = ProjectAuthorization.RequireWritableCassetteRight(
            access,
            CassetteRights.WriteMetadata);

        Assert.Equal(new[] { "allowed" }, allowed);
    }

    [Fact]
    public void RequireWritableCassetteRight_RejectsViewer()
    {
        ProjectAccessSnapshot access = Snapshot(
            isMember: true,
            projectRights: [ProjectRights.Read],
            readableCassette: true);

        ProjectAuthorizationException exception = Assert.Throws<ProjectAuthorizationException>(
            () => ProjectAuthorization.RequireWritableCassetteRight(
                access,
                CassetteRights.WriteMetadata));

        Assert.Equal(CassetteRights.WriteMetadata, exception.RequiredRight);
    }

    [Fact]
    public void RequireWritableCassetteRight_RequiresProjectRead()
    {
        ProjectAccessSnapshot access = new(
            "user",
            IsMember: true,
            Rights(),
            new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
            {
                ["cassette"] = new(
                    "cassette",
                    Enabled: true,
                    AllowWrite: true,
                    Rights(CassetteRights.Read, CassetteRights.WriteMetadata))
            },
            DefaultWriteCassetteId: "cassette");

        ProjectAuthorizationException exception = Assert.Throws<ProjectAuthorizationException>(
            () => ProjectAuthorization.RequireWritableCassetteRight(
                access,
                CassetteRights.WriteMetadata));

        Assert.Equal(ProjectRights.Read, exception.RequiredRight);
    }

    private static ProjectAccessSnapshot Snapshot(
        bool isMember,
        IEnumerable<string> projectRights,
        bool readableCassette)
    {
        Dictionary<string, CassetteAccessSnapshot> cassettes = new(StringComparer.Ordinal);
        if (readableCassette)
        {
            cassettes.Add(
                "cassette",
                new CassetteAccessSnapshot(
                    "cassette",
                    Enabled: true,
                    AllowWrite: false,
                    new HashSet<string>(StringComparer.Ordinal) { CassetteRights.Read }));
        }

        return new ProjectAccessSnapshot(
            "user",
            isMember,
            projectRights.ToHashSet(StringComparer.Ordinal),
            cassettes,
            DefaultWriteCassetteId: null);
    }

    private static IReadOnlySet<string> Rights(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);
}
