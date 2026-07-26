using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectCassetteCommandResolverTests
{
    [Fact]
    public void Resolve_AllowsOnlyTheRequiredCassetteRight()
    {
        ProjectAccessSnapshot access = Access(
            new HashSet<string>([CassetteRights.Delete], StringComparer.Ordinal));
        ProjectCassetteCommandResolver resolver = new();

        Assert.Equal(
            "current",
            resolver.Resolve(access, CassetteRights.Delete));
        Assert.Throws<UnauthorizedAccessException>(() =>
            resolver.Resolve(access, CassetteRights.Substitute));
    }

    [Fact]
    public void Resolve_SeparatesDocumentAddAndReplaceRights()
    {
        ProjectAccessSnapshot access = Access(
            new HashSet<string>([CassetteRights.AddDocuments], StringComparer.Ordinal));
        ProjectCassetteCommandResolver resolver = new();

        Assert.Equal(
            "current",
            resolver.Resolve(access, CassetteRights.AddDocuments));
        Assert.Throws<UnauthorizedAccessException>(() =>
            resolver.Resolve(access, CassetteRights.ReplaceDocuments));
    }

    [Fact]
    public void Resolve_RejectsExplicitCassetteWithoutRight()
    {
        ProjectAccessSnapshot access = Access(
            new HashSet<string>([CassetteRights.Delete], StringComparer.Ordinal));

        Assert.Throws<UnauthorizedAccessException>(() =>
            new ProjectCassetteCommandResolver().Resolve(
                access,
                CassetteRights.Delete,
                "archive"));
    }

    private static ProjectAccessSnapshot Access(IReadOnlySet<string> currentRights) => new(
        "editor",
        IsMember: true,
        new HashSet<string>(StringComparer.Ordinal),
        new Dictionary<string, CassetteAccessSnapshot>(StringComparer.Ordinal)
        {
            ["current"] = new("current", true, true, currentRights),
            ["archive"] = new(
                "archive",
                true,
                true,
                new HashSet<string>(StringComparer.Ordinal))
        },
        DefaultWriteCassetteId: "current");
}
