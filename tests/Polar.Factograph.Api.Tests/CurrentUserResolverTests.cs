using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class CurrentUserResolverTests
{
    [Fact]
    public void Resolve_PrefersAuthenticatedNameIdentifier()
    {
        CurrentUserResolver resolver = CreateResolver(Environments.Development, "development-user");
        DefaultHttpContext context = new();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "claim-user")],
            authenticationType: "test"));

        Assert.Equal("claim-user", resolver.Resolve(context));
    }

    [Fact]
    public void Resolve_UsesConfiguredUserOnlyInDevelopment()
    {
        CurrentUserResolver resolver = CreateResolver(Environments.Development, "development-user");

        Assert.Equal("development-user", resolver.Resolve(new DefaultHttpContext()));
    }

    [Fact]
    public void Resolve_RejectsAnonymousProductionRequest()
    {
        CurrentUserResolver resolver = CreateResolver(Environments.Production, "development-user");

        Assert.Throws<ApiAuthenticationException>(() => resolver.Resolve(new DefaultHttpContext()));
    }

    private static CurrentUserResolver CreateResolver(
        string environmentName,
        string developmentUser)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:DevelopmentUserId"] = developmentUser
            })
            .Build();
        TestHostEnvironment environment = new() { EnvironmentName = environmentName };
        return new CurrentUserResolver(configuration, environment);
    }
}
