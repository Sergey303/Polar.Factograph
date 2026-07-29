using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Tests;

public sealed class IdentityJsonStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"factograph-identity-{Guid.NewGuid():N}");

    [Fact]
    public async Task Update_writes_complete_json_and_publishes_snapshot()
    {
        Directory.CreateDirectory(_root);
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData());
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityUser user = CreateUser("u-1", "sergey");

        IdentityData result = await store.UpdateAsync(current => current with
        {
            Users = [user]
        });

        Assert.Same(result, store.Current);
        await using FileStream stream = File.OpenRead(Path.Combine(_root, "identity.json"));
        IdentityData? persisted = await JsonSerializer.DeserializeAsync<IdentityData>(
            stream,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("u-1", Assert.Single(persisted!.Users).Id);
    }

    [Fact]
    public async Task Update_accepts_viewer_without_fog()
    {
        Directory.CreateDirectory(_root);
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData());
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityUser viewer = CreateUser("u-1", "reader") with
        {
            Roles = ["viewer"],
            Fog = null
        };

        IdentityData result = await store.UpdateAsync(current => current with
        {
            Users = [viewer]
        });

        IdentityUser stored = Assert.Single(result.Users);
        Assert.Equal(["viewer"], stored.Roles);
        Assert.Null(stored.Fog);
    }

    [Fact]
    public void Reload_replaces_valid_snapshot_but_keeps_previous_on_invalid_data()
    {
        Directory.CreateDirectory(_root);
        IdentityUser first = CreateUser("u-1", "sergey");
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData
        {
            Users = [first]
        });
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityUser second = CreateUser("u-2", "anna");

        monitor.Trigger(new IdentityData { Users = [second] });
        Assert.Equal("u-2", Assert.Single(store.Current.Users).Id);

        monitor.Trigger(new IdentityData { Users = [second, second] });
        Assert.Equal("u-2", Assert.Single(store.Current.Users).Id);
    }

    [Fact]
    public void Overlay_adds_local_user_roles_without_replacing_static_members()
    {
        Directory.CreateDirectory(_root);
        IdentityUser user = CreateUser("u-1", "sergey") with
        {
            Roles = ["editor"]
        };
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData
        {
            Users = [user]
        });
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityProjectMemberOverlay overlay = new(store);
        ProjectDefinition project = CreateProject();

        ProjectDefinition result = overlay.Apply(project, user.Id);

        MemberDefinition member = Assert.Single(
            result.Members,
            value => value.UserId == user.Id);
        Assert.Equal(["editor"], member.Roles);
        Assert.Contains(result.Members, value => value.UserId == "admin");
    }

    [Fact]
    public void Fog_resolver_selects_only_the_path_assigned_to_local_user()
    {
        Directory.CreateDirectory(_root);
        string cassettePath = Path.Combine(_root, "cassette");
        IdentityUser user = CreateUser("u-1", "sergey") with
        {
            Fog = new IdentityFogReference
            {
                CassetteId = "main",
                DocumentUri = "iiss://cassette/0001/0002",
                RelativePath = "originals/0001/0002.fog"
            }
        };
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData
        {
            Users = [user]
        });
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityFogSourceResolver resolver = new(store);
        ProjectDefinition project = CreateProject(cassettePath);
        FogSourceDescriptor assigned = CreateSource(
            cassettePath,
            "originals/0001/0002.fog",
            user.Id);
        FogSourceDescriptor other = CreateSource(
            cassettePath,
            "meta/cassette_current.fog",
            "admin");

        FogSourceDescriptor result = resolver.Resolve(
            project,
            [other, assigned],
            user.Id,
            "main");

        Assert.Equal(assigned.FogPath, result.FogPath);
    }

    [Fact]
    public void Fog_resolver_rejects_registered_viewer_without_fog()
    {
        Directory.CreateDirectory(_root);
        string cassettePath = Path.Combine(_root, "cassette");
        IdentityUser viewer = CreateUser("u-1", "reader") with
        {
            Roles = ["viewer"],
            Fog = null
        };
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData
        {
            Users = [viewer]
        });
        using IdentityJsonStore store = CreateStore(monitor);
        IdentityFogSourceResolver resolver = new(store);
        ProjectDefinition project = CreateProject(cassettePath);
        FogSourceDescriptor legacyWritable = CreateSource(
            cassettePath,
            "meta/cassette_current.fog",
            "admin");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            resolver.Resolve(project, [legacyWritable], viewer.Id, "main"));

        Assert.Contains("not an editor", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private IdentityJsonStore CreateStore(
        IOptionsMonitor<IdentityData> monitor) => new(
        monitor,
        new LocalAuthenticationOptions(
            Path.Combine(_root, "identity.json"),
            Path.Combine(_root, "keys"),
            "test-session",
            "main",
            RegistrationEnabled: true,
            SessionDays: 30,
            MaxFogBytes: 1024 * 1024,
            EditorLogins: new HashSet<string>(StringComparer.Ordinal)),
        NullLogger<IdentityJsonStore>.Instance);

    private static IdentityUser CreateUser(string id, string login) => new()
    {
        Id = id,
        Login = login,
        NormalizedLogin = login.ToUpperInvariant(),
        DisplayName = login,
        PasswordHash = "hashed-password",
        Roles = ["viewer"],
        Fog = new IdentityFogReference
        {
            CassetteId = "main",
            DocumentUri = $"iiss://cassette/{id}",
            RelativePath = $"originals/0001/{id}.fog"
        },
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private ProjectDefinition CreateProject(string? cassettePath = null) => new()
    {
        ProjectId = "test",
        Name = "Test",
        Ontology = new OntologyDefinition { Path = Path.Combine(_root, "ontology.xml") },
        Index = new IndexDefinition { Path = Path.Combine(_root, "index") },
        Cassettes =
        [
            new CassetteDefinition
            {
                Id = "main",
                Name = "cassette",
                Path = cassettePath ?? Path.Combine(_root, "cassette"),
                AllowWrite = true
            }
        ],
        Roles = new Dictionary<string, RoleDefinition>(StringComparer.Ordinal)
        {
            ["editor"] = new RoleDefinition
            {
                ProjectRights = [ProjectRights.Read],
                CassetteRights = new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["main"] = [CassetteRights.Read, CassetteRights.WriteMetadata]
                }
            }
        },
        Members =
        [
            new MemberDefinition
            {
                UserId = "admin",
                Roles = ["editor"]
            }
        ]
    };

    private static FogSourceDescriptor CreateSource(
        string cassettePath,
        string relativePath,
        string owner) => new(
        "main",
        "cassette",
        Path.GetFullPath(relativePath, cassettePath),
        owner,
        null,
        owner,
        "test_",
        1000,
        Writable: true,
        IsCassetteMetadata: false,
        Length: 0,
        LastWriteTimeUtc: DateTime.UtcNow);

    private sealed class TestOptionsMonitor<T>(T current) : IOptionsMonitor<T>
    {
        private Action<T, string?>? _listener;

        public T CurrentValue { get; private set; } = current;

        public T Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<T, string?> listener)
        {
            _listener += listener;
            return new Subscription(() => _listener -= listener);
        }

        public void Trigger(T value)
        {
            CurrentValue = value;
            _listener?.Invoke(value, Options.DefaultName);
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
