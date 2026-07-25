namespace Polar.Factograph.Api.Authentication;

public sealed record JwtAuthenticationSettings(
    string Authority,
    string Audience,
    bool RequireHttpsMetadata)
{
    private const string Section = "Authentication:Jwt";

    public static JwtAuthenticationSettings? Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? authority = configuration[$"{Section}:Authority"];
        string? audience = configuration[$"{Section}:Audience"];
        bool requireHttps = configuration.GetValue($"{Section}:RequireHttpsMetadata", true);

        if (string.IsNullOrWhiteSpace(authority) && string.IsNullOrWhiteSpace(audience))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(authority) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException(
                $"Both {Section}:Authority and {Section}:Audience must be configured together.");
        }

        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri))
        {
            throw new InvalidOperationException($"{Section}:Authority must be an absolute URI.");
        }

        if (requireHttps && !string.Equals(authorityUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{Section}:Authority must use HTTPS when RequireHttpsMetadata is enabled.");
        }

        return new JwtAuthenticationSettings(authorityUri.AbsoluteUri.TrimEnd('/'), audience, requireHttps);
    }
}
