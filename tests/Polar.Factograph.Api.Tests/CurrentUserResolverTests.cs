using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class CurrentUserResolverTests
{
    [Fact]
    public void Resolve_PrefersAuthenticatedNameIdentifier()
    {
        CurrentUserResolver resolver = CreateResolver(
            Environments.Development,
            "development-user",
            publicReadEnabled: true);
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "claim-user")],
            authenticationType: "test"));

        Assert.Equal("claim-user", resolver.Resolve(context));
    }

    [Fact]
    public void Resolve_UsesConfiguredUserOnlyInDevelopment()
    {
        CurrentUserResolver resolver = CreateResolver(
            Environments.Development,
            "development-user",
            publicReadEnabled: true);

        Assert.Equal("development-user", resolver.Resolve(new DefaultHttpContext()));
    }

    [Fact]
    public void Resolve_UsesPublicViewerForAnonymousProductionRequest()
    {
        LocalAuthenticationOptions options = CreateOptions(publicReadEnabled: true);
        CurrentUserResolver resolver = CreateResolver(
            Environments.Production,
            "development-user",
            options);

        Assert.Equal(options.PublicUserId, resolver.Resolve(new DefaultHttpContext()));
    }

    [Fact]
    public void Resolve_RejectsAnonymousProductionRequestWhenPublicReadingIsDisabled()
    {
        CurrentUserResolver resolver = CreateResolver(
            Environments.Production,
            "development-user",
            publicReadEnabled: false);

        Assert.Throws<ApiAuthenticationException>(() =>
            resolver.Resolve(new DefaultHttpContext()));
    }

    private static CurrentUserResolver CreateResolver(
        string environmentName,
        string developmentUser,
        bool publicReadEnabled) => CreateResolver(
        environmentName,
        developmentUser,
        CreateOptions(publicReadEnabled));

    private static CurrentUserResolver CreateResolver(
        string environmentName,
        string developmentUser,
        LocalAuthenticationOptions options)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:DevelopmentUserId"] = developmentUser
            })
            .Build();
        TestHostEnvironment environment = new() { EnvironmentName = environmentName };
        return new CurrentUserResolver(configuration, environment, options);
    }

    private static LocalAuthenticationOptions CreateOptions(bool publicReadEnabled) => new(
        Path.Combine(Path.GetTempPath(), "factograph-test-identity.json"),
        Path.Combine(Path.GetTempPath(), "factograph-test-keys"),
        "test-session",
        "main",
        RegistrationEnabled: true,
        SessionDays: 30,
        MaxFogBytes: 1024 * 1024,
        EditorLogins: new HashSet<string>(StringComparer.Ordinal))
    {
        PublicReadEnabled = publicReadEnabled,
        PublicUserId = LocalAuthenticationOptions.DefaultPublicUserId
    };
}
