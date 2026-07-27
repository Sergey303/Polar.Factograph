using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/browser", GetBrowserConfiguration);
        return endpoints;
    }

    private static IResult GetBrowserConfiguration(IConfiguration configuration)
    {
        BrowserAuthenticationSettings? settings =
            BrowserAuthenticationSettings.Read(configuration);
        return Results.Ok(settings is null
            ? new BrowserAuthenticationConfiguration(false, null, null, null)
            : new BrowserAuthenticationConfiguration(
                true,
                settings.Authority,
                settings.ClientId,
                settings.Scope));
    }
}

public sealed record BrowserAuthenticationConfiguration(
    bool Enabled,
    string? Authority,
    string? ClientId,
    string? Scope);
