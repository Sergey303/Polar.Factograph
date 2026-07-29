using Microsoft.Extensions.FileProviders;
using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Tests;

public sealed class LocalAuthenticationOptionsTests
{
    [Fact]
    public void Read_normalizes_editor_logins_and_marks_allow_list_configured()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:EditorLogins:0"] = "  Сергей  ",
                ["Authentication:Local:EditorLogins:1"] = "Editor.Two"
            })
            .Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.True(options.EditorAllowListConfigured);
        Assert.Equal(2, options.EditorLogins.Count);
        Assert.True(options.IsEditor(LocalLoginName.Normalize("сергей")));
        Assert.True(options.IsEditor(LocalLoginName.Normalize("EDITOR.TWO")));
        Assert.False(options.IsEditor(LocalLoginName.Normalize("reader")));
    }

    [Fact]
    public void Read_rejects_duplicate_editor_logins_after_normalization()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Local:EditorLogins:0"] = "Sergey",
                ["Authentication:Local:EditorLogins:1"] = "sergey"
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            LocalAuthenticationOptions.Read(configuration, new TestHostEnvironment()));

        Assert.Contains("duplicate login", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_keeps_legacy_mode_when_editor_list_is_absent()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.False(options.EditorAllowListConfigured);
        Assert.Empty(options.EditorLogins);
        Assert.Equal("editor", options.DefaultRole);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
