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
}