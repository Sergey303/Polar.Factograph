using Microsoft.Extensions.FileProviders;
using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Tests;

public sealed class LocalAuthenticationOptionsTests
{
    [Fact]
    public void Read_normalizes_editor_logins()
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
    public void Read_treats_absent_editor_list_as_empty()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            new TestHostEnvironment());

        Assert.Empty(options.EditorLogins);
        Assert.False(options.IsEditor(LocalLoginName.Normalize("reader")));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
