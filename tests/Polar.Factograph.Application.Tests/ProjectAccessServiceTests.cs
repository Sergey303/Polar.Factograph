using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Xunit;

namespace Polar.Factograph.Application.Tests;

public sealed class ProjectAccessServiceTests
{
    [Fact]
    public void Evaluate_UnionsRolesAppliesDefaultsAndRemovesWritesFromReadOnlyCassette()
    {
        ProjectDefinition project = CreateProject(
            cassettes:
            [
                Cassette("history", defaultAccess: "read", allowWrite: false),
                Cassette("current", defaultAccess: "none", allowWrite: true)
            ],
            roles: new Dictionary<string, RoleDefinition>(StringComparer.Ordinal)
            {
                ["viewer"] = new()
                {
                    ProjectRights = [ProjectRights.Read, ProjectRights.Search]
                },
                ["editor"] = new()
                {
                    CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["*"] = [CassetteRights.Read],
                        ["history"] = [CassetteRights.WriteMetadata],
                        ["current"] = [CassetteRights.WriteMetadata, CassetteRights.AddDocuments]
                    }
                }
            },
            members:
            [
                new MemberDefinition
                {
                    UserId = "member",
                    Roles = ["viewer", "editor"]
                }
            ],
            defaultCassetteByRole: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["editor"] = "current"
            });

        ProjectAccessSnapshot access = new ProjectAccessService().Evaluate(project, "member");

        Assert.True(access.IsMember);
        Assert.True(access.HasProjectRight(ProjectRights.Read));
        Assert.True(access.HasProjectRight(ProjectRights.Search));
        Assert.True(access.HasCassetteRight("history", CassetteRights.Read));
        Assert.False(access.HasCassetteRight("history", CassetteRights.WriteMetadata));
        Assert.True(access.HasCassetteRight("current", CassetteRights.Read));
        Assert.True(access.HasCassetteRight("current", CassetteRights.WriteMetadata));
        Assert.True(access.HasCassetteRight("current", CassetteRights.AddDocuments));
        Assert.Equal(new[] { "current", "history" }, access.ReadableCassetteIds.Order(StringComparer.Ordinal));
        Assert.Equal("current", access.DefaultWriteCassetteId);
    }

    [Fact]
    public void Evaluate_ExactMemberOverrideReplacesRoleAndWildcardRights()
    {
        ProjectDefinition project = CreateProject(
            cassettes:
            [
                Cassette("a", defaultAccess: "read", allowWrite: true),
                Cassette("b", defaultAccess: "read", allowWrite: true)
            ],
            roles: new Dictionary<string, RoleDefinition>(StringComparer.Ordinal)
            {
                ["administrator"] = new()
                {
                    ProjectRights = [ProjectRights.Read],
                    CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["*"] = [CassetteRights.Read, CassetteRights.WriteMetadata, CassetteRights.Delete]
                    }
                }
            },
            members:
            [
                new MemberDefinition
                {
                    UserId = "restricted-admin",
                    Roles = ["administrator"],
                    CassetteOverrides = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["*"] = [CassetteRights.Read],
                        ["b"] = []
                    }
                }
            ]);

        ProjectAccessSnapshot access = new ProjectAccessService().Evaluate(project, "restricted-admin");

        Assert.True(access.HasCassetteRight("a", CassetteRights.Read));
        Assert.False(access.HasCassetteRight("a", CassetteRights.WriteMetadata));
        Assert.False(access.HasCassetteRight("a", CassetteRights.Delete));
        Assert.False(access.HasCassetteRight("b", CassetteRights.Read));
        Assert.Equal(new[] { "a" }, access.ReadableCassetteIds);
    }

    [Fact]
    public void Evaluate_DisabledCassetteHasNoRightsAndCannotBeDefaultWriteTarget()
    {
        ProjectDefinition project = CreateProject(
            cassettes:
            [
                Cassette("disabled", defaultAccess: "read", allowWrite: true, enabled: false)
            ],
            roles: new Dictionary<string, RoleDefinition>(StringComparer.Ordinal)
            {
                ["editor"] = new()
                {
                    ProjectRights = [ProjectRights.Read],
                    CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                    {
                        ["disabled"] = [CassetteRights.Read, CassetteRights.WriteMetadata]
                    }
                }
            },
            members:
            [
                new MemberDefinition
                {
                    UserId = "editor-user",
                    Roles = ["editor"]
                }
            ],
            defaultCassetteByRole: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["editor"] = "disabled"
            });

        ProjectAccessSnapshot access = new ProjectAccessService().Evaluate(project, "editor-user");

        Assert.Empty(access.ReadableCassetteIds);
        Assert.False(access.HasCassetteRight("disabled", CassetteRights.Read));
        Assert.Null(access.DefaultWriteCassetteId);
    }

    [Fact]
    public void Evaluate_UnknownUserReceivesNoProjectOrCassetteAccess()
    {
        ProjectDefinition project = CreateProject(
            cassettes: [Cassette("a", defaultAccess: "read", allowWrite: true)],
            roles: new Dictionary<string, RoleDefinition>(StringComparer.Ordinal),
            members: Array.Empty<MemberDefinition>());

        ProjectAccessSnapshot access = new ProjectAccessService().Evaluate(project, "unknown");

        Assert.False(access.IsMember);
        Assert.Empty(access.ProjectRights);
        Assert.Empty(access.Cassettes);
        Assert.Empty(access.ReadableCassetteIds);
        Assert.Null(access.DefaultWriteCassetteId);
    }

    private static ProjectDefinition CreateProject(
        CassetteDefinition[] cassettes,
        Dictionary<string, RoleDefinition> roles,
        MemberDefinition[] members,
        Dictionary<string, string>? defaultCassetteByRole = null) => new()
    {
        ProjectId = "project",
        Name = "Project",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = "index" },
        Cassettes = cassettes,
        Roles = roles,
        Members = members,
        WriteRouting = new WriteRoutingDefinition
        {
            DefaultCassetteByRole = defaultCassetteByRole
                ?? new Dictionary<string, string>(StringComparer.Ordinal)
        }
    };

    private static CassetteDefinition Cassette(
        string id,
        string defaultAccess,
        bool allowWrite,
        bool enabled = true) => new()
    {
        Id = id,
        Name = id,
        Path = id,
        Enabled = enabled,
        DefaultAccess = defaultAccess,
        AllowWrite = allowWrite
    };
}