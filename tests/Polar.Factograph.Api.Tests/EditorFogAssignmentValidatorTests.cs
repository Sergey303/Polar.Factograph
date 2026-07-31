using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Tests;

public sealed class EditorFogAssignmentValidatorTests
{
    [Fact]
    public void Validate_AcceptsRegisteredEditorAndReportsUnregisteredConfiguration()
    {
        string cassettePath = Path.Combine(Path.GetTempPath(), "factograph-editor-fog");
        const string relativeFogPath = "originals/0001/editor.fog";
        ProjectDefinition project = CreateProject(cassettePath);
        IdentityData identity = new()
        {
            Users =
            [
                CreateEditor("u_editor", "editor", relativeFogPath)
            ]
        };
        LocalAuthenticationOptions options = CreateOptions("editor", "future-editor");
        FogSourceDescriptor[] sources =
        [
            CreateSource(cassettePath, relativeFogPath, "u_editor"),
            CreateSource(cassettePath, "originals/0001/legacy.fog", "legacy-owner")
        ];

        EditorFogValidationStatistics result = EditorFogAssignmentValidator.Validate(
            project,
            sources,
            identity,
            options);

        Assert.Equal(2, result.ConfiguredEditors);
        Assert.Equal(1, result.RegisteredEditors);
        Assert.Equal(1, result.UnregisteredEditors);
        Assert.Equal(1, result.EditorsWithFog);
        Assert.Equal(1, result.ValidEditorFogs);
        Assert.Equal(0, result.InvalidEditorFogs);
        Assert.Equal(1, result.UnassignedWritableFogs);
    }

    [Fact]
    public void Validate_RejectsRegisteredEditorWhoseFogUsesAnOldCassetteId()
    {
        string cassettePath = Path.Combine(Path.GetTempPath(), "factograph-editor-fog");
        ProjectDefinition project = CreateProject(cassettePath);
        IdentityUser editor = CreateEditor(
            "u_editor",
            "editor",
            "originals/0001/editor.fog") with
        {
            Fog = new IdentityFogReference
            {
                CassetteId = "old-cassette",
                DocumentUri = "iiss://old-cassette@0001/1",
                RelativePath = "originals/0001/editor.fog"
            }
        };
        IdentityData identity = new() { Users = [editor] };
        LocalAuthenticationOptions options = CreateOptions("editor");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            EditorFogAssignmentValidator.Validate(
                project,
                Array.Empty<FogSourceDescriptor>(),
                identity,
                options));

        Assert.Contains("editor", exception.Message, StringComparison.Ordinal);
    }

    private static ProjectDefinition CreateProject(string cassettePath) => new()
    {
        ProjectId = "project",
        Name = "Project",
        Ontology = new OntologyDefinition { Path = "ontology.xml" },
        Index = new IndexDefinition { Path = "index" },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "SypCassete",
                Name = "SypCassete",
                Path = cassettePath,
                Enabled = true,
                AllowWrite = true
            }
        ]
    };

    private static IdentityUser CreateEditor(
        string id,
        string login,
        string relativeFogPath) => new()
    {
        Id = id,
        Login = login,
        NormalizedLogin = login,
        DisplayName = login,
        PasswordHash = "hash",
        Roles = ["editor"],
        Fog = new IdentityFogReference
        {
            CassetteId = "SypCassete",
            DocumentUri = "iiss://SypCassete@0001/1",
            RelativePath = relativeFogPath
        }
    };

    private static LocalAuthenticationOptions CreateOptions(params string[] editors) => new(
        IdentityPath: "identity.json",
        DataProtectionKeysPath: "keys",
        CookieName: "cookie",
        DefaultCassetteId: "SypCassete",
        RegistrationEnabled: true,
        SessionDays: 30,
        MaxFogBytes: 1024,
        EditorLogins: editors.ToHashSet(StringComparer.Ordinal));

    private static FogSourceDescriptor CreateSource(
        string cassettePath,
        string relativeFogPath,
        string owner) => new(
            CassetteId: "SypCassete",
            CassetteName: "SypCassete",
            FogPath: Path.GetFullPath(relativeFogPath, cassettePath),
            DatabaseId: owner,
            CassetteUri: null,
            Owner: owner,
            Prefix: owner + "_",
            Counter: 1000,
            Writable: true,
            IsCassetteMetadata: false,
            Length: 1,
            LastWriteTimeUtc: DateTime.UtcNow);
}
