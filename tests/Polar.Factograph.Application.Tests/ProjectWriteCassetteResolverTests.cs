using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectWriteCassetteResolverTests
{
    [Fact]
    public void Resolve_UsesAuthorizedDefaultCassette()
    {
        ProjectAccessSnapshot access = Access(
            defaultCassetteId: "current",
            ("current", true));

        string result = new ProjectWriteCassetteResolver().Resolve(access);

        Assert.Equal("current", result);
    }

    [Fact]
    public void Resolve_AllowsExplicitAuthorizedCassette()
    {
        ProjectAccessSnapshot access = Access(
            defaultCassetteId: "current",
            ("current", true),
            ("archive", true));

        string result = new ProjectWriteCassetteResolver().Resolve(access, "archive");

        Assert.Equal("archive", result);
    }

    [Fact]
    public void Resolve_RejectsCassetteWithoutMetadataWriteRight()
    {
        ProjectAccessSnapshot access = Access(
            defaultCassetteId: "current",
            ("current", true),
            ("archive", false));

        CassetteAuthorizationException exception = Assert.Throws<CassetteAuthorizationException>(() =>
            new ProjectWriteCassetteResolver().Resolve(access, "archive"));

        Assert.Equal("user", exception.UserId);
        Assert.Equal("archive", exception.CassetteId);
        Assert.Equal(CassetteRights.WriteMetadata, exception.RequiredRight);
    }

    [Fact]
    public void Resolve_RejectsMissingDefaultCassette()
    {
        ProjectAccessSnapshot access = Access(defaultCassetteId: null);

        Assert.Throws<InvalidOperationException>(() =>
            new ProjectWriteCassetteResolver().Resolve(access));
    }

    private static ProjectAccessSnapshot Access(
        string? defaultCassetteId,
        params (string Id, bool CanWrite)[] cassettes) => new(
        "user",
        IsMember: true,
        new HashSet<string>(StringComparer.Ordinal),
        cassettes.ToDictionary(
            cassette => cassette.Id,
            cassette => new CassetteAccessSnapshot(
                cassette.Id,
                Enabled: true,
                AllowWrite: true,
                cassette.CanWrite
                    ? new HashSet<string>([CassetteRights.WriteMetadata], StringComparer.Ordinal)
                    : new HashSet<string>(StringComparer.Ordinal)),
            StringComparer.Ordinal),
        defaultCassetteId);
}
