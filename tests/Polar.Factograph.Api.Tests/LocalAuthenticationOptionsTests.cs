using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Tests;

public sealed class LocalAuthenticationOptionsTests
{
    [Fact]
    public void Read_normalizes_editor_and_admin_logins()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:EditorLogins:0"] = "  Сергей  ",
                ["Authentication:Local:EditorLogins:1"] = "Editor.Two",
                ["Authentication:Local:AdminLogins:0"] = "  ADMIN  "
            })
            .Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.Equal(2, options.EditorLogins.Count);
        Assert.True(options.IsEditor(LocalLoginName.Normalize("сергей")));
        Assert.True(options.IsEditor(LocalLoginName.Normalize("EDITOR.TWO")));
        Assert.False(options.IsEditor(LocalLoginName.Normalize("reader")));
        Assert.Single(options.AdminLogins);
        Assert.True(options.IsAdministrator(LocalLoginName.Normalize("admin")));
        Assert.False(options.IsAdministrator(LocalLoginName.Normalize("reader")));
    }

    [Fact]
    public void Read_allows_the_same_login_in_editor_and_admin_lists()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:EditorLogins:0"] = "admin",
                ["Authentication:Local:AdminLogins:0"] = "ADMIN"
            })
            .Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        string login = LocalLoginName.Normalize("admin");
        Assert.True(options.IsEditor(login));
        Assert.True(options.IsAdministrator(login));
    }

    [Theory]
    [InlineData("EditorLogins")]
    [InlineData("AdminLogins")]
    public void Read_rejects_duplicate_logins_after_normalization(string settingName)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Authentication:Local:{settingName}:0"] = "Sergey",
                [$"Authentication:Local:{settingName}:1"] = "sergey"
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            LocalAuthenticationOptions.Read(configuration, new TestHostEnvironment()));

        Assert.Contains("duplicate login", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_treats_absent_role_lists_as_empty()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.Empty(options.EditorLogins);
        Assert.Empty(options.AdminLogins);
        Assert.False(options.IsEditor(LocalLoginName.Normalize("reader")));
        Assert.False(options.IsAdministrator(LocalLoginName.Normalize("reader")));
    }

    [Fact]
    public void Read_disables_public_reading_by_default()
    {
        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            new ConfigurationBuilder().Build(),
            new TestHostEnvironment());

        Assert.False(options.PublicReadEnabled);
        Assert.Equal(LocalAuthenticationOptions.DefaultPublicUserId, options.PublicUserId);
        Assert.False(options.IsPublicUser(options.PublicUserId));
    }

    [Fact]
    public void Read_configures_stable_public_identity()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:PublicReadEnabled"] = "true",
                ["Authentication:Local:PublicUserId"] = "  public-catalog  "
            })
            .Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.True(options.PublicReadEnabled);
        Assert.Equal("public-catalog", options.PublicUserId);
        Assert.True(options.IsPublicUser("public-catalog"));
        Assert.False(options.IsPublicUser("other"));
    }

    [Fact]
    public void Read_rejects_empty_public_identity_when_public_reading_is_enabled()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:PublicReadEnabled"] = "true",
                ["Authentication:Local:PublicUserId"] = "   "
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            LocalAuthenticationOptions.Read(configuration, new TestHostEnvironment()));

        Assert.Contains("PublicUserId", exception.Message, StringComparison.Ordinal);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
