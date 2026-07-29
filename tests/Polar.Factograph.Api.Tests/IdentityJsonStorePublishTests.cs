using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Tests;

public sealed class IdentityJsonStorePublishTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(),
        $"factograph-identity-publish-{Guid.NewGuid():N}");

    [Fact]
    public async Task Update_retries_when_identity_file_is_temporarily_locked()
    {
        Directory.CreateDirectory(_root);
        string identityPath = Path.Combine(_root, "identity.json");
        await File.WriteAllTextAsync(identityPath, "{}");
        TestOptionsMonitor<IdentityData> monitor = new(new IdentityData());
        using IdentityJsonStore store = new(
            monitor,
            new LocalAuthenticationOptions(
                identityPath,
                Path.Combine(_root, "keys"),
                "test-session",
                "main",
                RegistrationEnabled: true,
                SessionDays: 30,
                MaxFogBytes: 1024 * 1024,
                EditorLogins: new HashSet<string>(StringComparer.Ordinal)),
            NullLogger<IdentityJsonStore>.Instance);

        using FileStream locked = new(
            identityPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        Task<IdentityData> update = store.UpdateAsync(current => current);

        await Task.Delay(100);
        locked.Dispose();
        IdentityData result = await update;

        Assert.Same(result, store.Current);
        await using FileStream persisted = File.OpenRead(identityPath);
        IdentityData? value = await JsonSerializer.DeserializeAsync<IdentityData>(
            persisted,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(1, value!.SchemaVersion);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestOptionsMonitor<T>(T current) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = current;

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
