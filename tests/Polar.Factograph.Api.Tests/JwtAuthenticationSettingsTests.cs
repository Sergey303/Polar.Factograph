using Microsoft.Extensions.Configuration;
using Polar.Factograph.Api.Authentication;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class JwtAuthenticationSettingsTests
{
    [Fact]
    public void Read_ReturnsNullWhenJwtIsNotConfigured()
    {
        IConfiguration configuration = Build();

        Assert.Null(JwtAuthenticationSettings.Read(configuration));
    }

    [Fact]
    public void Read_ReturnsValidatedSettings()
    {
        IConfiguration configuration = Build(
            ("Authentication:Jwt:Authority", "https://identity.example.test/"),
            ("Authentication:Jwt:Audience", "polar-factograph"));

        JwtAuthenticationSettings settings = Assert.IsType<JwtAuthenticationSettings>(
            JwtAuthenticationSettings.Read(configuration));

        Assert.Equal("https://identity.example.test", settings.Authority);
        Assert.Equal("polar-factograph", settings.Audience);
        Assert.True(settings.RequireHttpsMetadata);
    }

    [Theory]
    [InlineData("Authentication:Jwt:Authority", "https://identity.example.test")]
    [InlineData("Authentication:Jwt:Audience", "polar-factograph")]
    public void Read_RejectsIncompleteConfiguration(string key, string value)
    {
        IConfiguration configuration = Build((key, value));

        Assert.Throws<InvalidOperationException>(() =>
            JwtAuthenticationSettings.Read(configuration));
    }

    [Fact]
    public void Read_RejectsHttpAuthorityWhenHttpsIsRequired()
    {
        IConfiguration configuration = Build(
            ("Authentication:Jwt:Authority", "http://identity.example.test"),
            ("Authentication:Jwt:Audience", "polar-factograph"));

        Assert.Throws<InvalidOperationException>(() =>
            JwtAuthenticationSettings.Read(configuration));
    }

    [Fact]
    public void Read_AllowsHttpAuthorityWhenHttpsMetadataIsDisabled()
    {
        IConfiguration configuration = Build(
            ("Authentication:Jwt:Authority", "http://localhost:8080"),
            ("Authentication:Jwt:Audience", "polar-factograph"),
            ("Authentication:Jwt:RequireHttpsMetadata", "false"));

        JwtAuthenticationSettings settings = Assert.IsType<JwtAuthenticationSettings>(
            JwtAuthenticationSettings.Read(configuration));

        Assert.False(settings.RequireHttpsMetadata);
    }

    private static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(item => item.Key, item => (string?)item.Value))
            .Build();
}
