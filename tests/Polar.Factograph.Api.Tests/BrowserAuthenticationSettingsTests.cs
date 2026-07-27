using Microsoft.Extensions.Configuration;
using Polar.Factograph.Api.Authentication;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class BrowserAuthenticationSettingsTests
{
    [Fact]
    public void Read_ReturnsNullWhenBrowserLoginIsNotConfigured()
    {
        Assert.Null(BrowserAuthenticationSettings.Read(Build()));
    }

    [Fact]
    public void Read_UsesValidatedJwtAuthority()
    {
        IConfiguration configuration = Build(
            ("Authentication:Jwt:Authority", "https://identity.example.test/"),
            ("Authentication:Jwt:Audience", "polar-factograph"),
            ("Authentication:Browser:ClientId", "factograph-web"),
            ("Authentication:Browser:Scope", "openid   profile api"));

        BrowserAuthenticationSettings settings = Assert.IsType<BrowserAuthenticationSettings>(
            BrowserAuthenticationSettings.Read(configuration));

        Assert.Equal("https://identity.example.test", settings.Authority);
        Assert.Equal("factograph-web", settings.ClientId);
        Assert.Equal("openid profile api", settings.Scope);
    }

    [Theory]
    [InlineData("Authentication:Browser:ClientId", "factograph-web")]
    [InlineData("Authentication:Browser:Scope", "openid api")]
    public void Read_RejectsIncompleteBrowserConfiguration(string key, string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            BrowserAuthenticationSettings.Read(Build((key, value))));
    }

    [Fact]
    public void Read_RejectsBrowserLoginWithoutJwtValidation()
    {
        IConfiguration configuration = Build(
            ("Authentication:Browser:ClientId", "factograph-web"),
            ("Authentication:Browser:Scope", "openid api"));

        Assert.Throws<InvalidOperationException>(() =>
            BrowserAuthenticationSettings.Read(configuration));
    }

    private static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
}
