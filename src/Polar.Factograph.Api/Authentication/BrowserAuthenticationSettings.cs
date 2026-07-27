namespace Polar.Factograph.Api.Authentication;

public sealed record BrowserAuthenticationSettings(
    string Authority,
    string ClientId,
    string Scope)
{
    private const string Section = "Authentication:Browser";

    public static BrowserAuthenticationSettings? Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        string? clientId = configuration[$"{Section}:ClientId"];
        string? scope = configuration[$"{Section}:Scope"];

        if (string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(scope))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(scope))
        {
            throw new InvalidOperationException(
                $"Both {Section}:ClientId and {Section}:Scope must be configured together.");
        }

        JwtAuthenticationSettings jwt = JwtAuthenticationSettings.Read(configuration)
            ?? throw new InvalidOperationException(
                $"{Section} requires Authentication:Jwt to be configured.");
        return new BrowserAuthenticationSettings(
            jwt.Authority,
            clientId.Trim(),
            string.Join(' ', scope.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }
}
