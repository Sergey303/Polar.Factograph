using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using LocalIdentityUser = Polar.Factograph.Api.Authentication.IdentityUser;

namespace Polar.Factograph.Api.Tests;

public sealed class LocalAuthenticationServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"factograph-local-auth-{Guid.NewGuid():N}");

    [Fact]
    public async Task Register_creates_fog_only_for_configured_editor()
    {
        TestContext context = await CreateContextAsync(["editor-one"]);
        using (context.Store)
        {
            LocalAuthenticationSession viewer = await context.Service.RegisterAsync(
                "reader-one",
                "reader-password",
                displayName: null,
                deviceName: null);
            LocalAuthenticationSession editor = await context.Service.RegisterAsync(
                "EDITOR-ONE",
                "editor-password",
                displayName: null,
                deviceName: null);

            Assert.Equal(["viewer"], viewer.User.Roles);
            Assert.Null(viewer.User.Fog);
            Assert.Equal(["editor"], editor.User.Roles);
            Assert.NotNull(editor.User.Fog);
            Assert.Equal(1, context.Writer.WriteCount);
        }
    }

    [Fact]
    public async Task Register_assigns_editor_and_administrator_roles_independently()
    {
        TestContext context = await CreateContextAsync(
            editorLogins: ["admin"],
            adminLogins: ["ADMIN"]);
        using (context.Store)
        {
            LocalAuthenticationSession admin = await context.Service.RegisterAsync(
                "admin",
                "administrator-password",
                displayName: null,
                deviceName: null);

            Assert.Equal(["editor", "administrator"], admin.User.Roles);
            Assert.NotNull(admin.User.Fog);
            Assert.Equal(1, context.Writer.WriteCount);
        }
    }

    [Fact]
    public async Task ProvisionConfiguredEditors_creates_missing_fog_and_demotes_other_users()
    {
        LocalIdentityUser editor = CreateUser("u-editor", "editor-one", ["viewer"], fog: null);
        LocalIdentityUser formerEditor = CreateUser(
            "u-former",
            "former-editor",
            ["editor"],
            new IdentityFogReference
            {
                CassetteId = "main",
                DocumentUri = "iiss://main/0001/old",
                RelativePath = "originals/0001/old.fog"
            });
        TestContext context = await CreateContextAsync(
            ["editor-one"],
            identity: new IdentityData { Users = [editor, formerEditor] });
        using (context.Store)
        {
            await context.Service.ProvisionConfiguredEditorsAsync();

            LocalIdentityUser updatedEditor = context.Store.FindUser(editor.Id)!;
            LocalIdentityUser updatedFormer = context.Store.FindUser(formerEditor.Id)!;
            Assert.Equal(["editor"], updatedEditor.Roles);
            Assert.NotNull(updatedEditor.Fog);
            Assert.True(File.Exists(Path.Combine(
                context.CassettePath,
                updatedEditor.Fog!.RelativePath)));
            Assert.Equal(["viewer"], updatedFormer.Roles);
            Assert.Null(updatedFormer.Fog);
            Assert.Equal(1, context.Writer.WriteCount);
        }
    }

    [Fact]
    public async Task ProvisionConfiguredEditors_promotes_existing_admin_login()
    {
        LocalIdentityUser user = CreateUser("u-admin", "admin", ["viewer"], fog: null);
        TestContext context = await CreateContextAsync(
            editorLogins: ["admin"],
            adminLogins: ["admin"],
            identity: new IdentityData { Users = [user] });
        using (context.Store)
        {
            await context.Service.ProvisionConfiguredEditorsAsync();

            LocalIdentityUser updated = context.Store.FindUser(user.Id)!;
            Assert.Equal(["editor", "administrator"], updated.Roles);
            Assert.NotNull(updated.Fog);
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private async Task<TestContext> CreateContextAsync(
        string[] editorLogins,
        string[]? adminLogins = null,
        IdentityData? identity = null)
    {
        Directory.CreateDirectory(_root);
        string cassettePath = Path.Combine(_root, "cassette");
        Directory.CreateDirectory(cassettePath);
        string projectPath = Path.Combine(_root, "project.json");
        await File.WriteAllTextAsync(projectPath, ProjectJson, Encoding.UTF8);

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Project:ConfigPath"] = projectPath
            })
            .Build();
        TestHostEnvironment environment = new() { ContentRootPath = _root };
        HashSet<string> normalizedEditors = editorLogins
            .Select(LocalLoginName.Normalize)
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> normalizedAdmins = (adminLogins ?? Array.Empty<string>())
            .Select(LocalLoginName.Normalize)
            .ToHashSet(StringComparer.Ordinal);
        LocalAuthenticationOptions options = new(
            Path.Combine(_root, "identity.json"),
            Path.Combine(_root, "keys"),
            "test-session",
            "main",
            RegistrationEnabled: true,
            SessionDays: 30,
            MaxFogBytes: 1024 * 1024,
            EditorLogins: normalizedEditors)
        {
            AdminLogins = normalizedAdmins
        };
        TestOptionsMonitor<IdentityData> monitor = new(identity ?? new IdentityData());
        IdentityJsonStore store = new(
            monitor,
            options,
            NullLogger<IdentityJsonStore>.Instance);
        TestNamedFogWriter writer = new(cassettePath);
        LocalAuthenticationService service = new(
            store,
            new PasswordHasher<LocalIdentityUser>(),
            options,
            new ProjectPathResolver(configuration, environment),
            new ProjectConfigurationLoader(),
            writer,
            NullLogger<LocalAuthenticationService>.Instance);
        return new TestContext(store, service, writer, cassettePath);
    }

    private static LocalIdentityUser CreateUser(
        string id,
        string login,
        string[] roles,
        IdentityFogReference? fog) => new()
    {
        Id = id,
        Login = login,
        NormalizedLogin = LocalLoginName.Normalize(login),
        DisplayName = login,
        PasswordHash = "hashed-password",
        Roles = roles,
        Fog = fog,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private const string ProjectJson = """
        {
          "schemaVersion": 1,
          "projectId": "test",
          "name": "Test",
          "ontology": { "path": "ontology.xml" },
          "index": {
            "path": "index",
            "rebuildMode": "whenSourcesChanged"
          },
          "cassettes": [
            {
              "id": "main",
              "name": "Main",
              "path": "cassette",
              "enabled": true,
              "defaultAccess": "read",
              "allowWrite": true
            }
          ],
          "roles": {
            "viewer": {
              "projectRights": ["read", "search"],
              "cassetteRights": { "main": ["read"] }
            },
            "editor": {
              "projectRights": ["read", "search"],
              "cassetteRights": {
                "main": ["read", "writeMetadata", "addDocuments", "replaceDocuments"]
              }
            },
            "administrator": {
              "projectRights": [
                "read",
                "search",
                "export",
                "manageUsers",
                "manageCassettes",
                "rebuildIndex"
              ],
              "cassetteRights": {
                "main": [
                  "read",
                  "writeMetadata",
                  "addDocuments",
                  "replaceDocuments",
                  "delete",
                  "substitute",
                  "manage"
                ]
              }
            }
          },
          "members": [],
          "writeRouting": {
            "defaultCassetteByRole": {
              "editor": "main",
              "administrator": "main"
            }
          }
        }
        """;

    private sealed record TestContext(
        IdentityJsonStore Store,
        LocalAuthenticationService Service,
        TestNamedFogWriter Writer,
        string CassettePath);

    private sealed class TestNamedFogWriter(string cassettePath) : ICassetteNamedFogWriter
    {
        public int WriteCount { get; private set; }

        public async Task<CassetteDocumentWriteResult> AddAsync(
            CassetteDefinition cassette,
            Stream content,
            string fileName,
            long maxBytes,
            CancellationToken cancellationToken = default)
        {
            WriteCount++;
            string folder = "0001";
            string documentNumber = $"{WriteCount:D4}";
            string storedName = $"{documentNumber}-{fileName}";
            string directory = Path.Combine(cassettePath, "originals", folder);
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, storedName);
            await using FileStream target = File.Create(path);
            await content.CopyToAsync(target, cancellationToken);
            return new CassetteDocumentWriteResult(
                cassette.Id,
                cassette.Name,
                $"iiss://{cassette.Id}/{folder}/{documentNumber}",
                folder,
                documentNumber,
                storedName,
                target.Length,
                "test-sha",
                Replaced: false);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestOptionsMonitor<T>(T current) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; private set; } = current;

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener) => new Subscription();
    }

    private sealed class Subscription : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
