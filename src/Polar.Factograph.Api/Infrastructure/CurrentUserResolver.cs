using System.Security.Claims;
using Polar.Factograph.Api.Authentication;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class CurrentUserResolver(
    IConfiguration configuration,
    IHostEnvironment environment,
    LocalAuthenticationOptions authentication)
{
    public string Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        string? developmentUser = environment.IsDevelopment()
            ? configuration["Api:DevelopmentUserId"]
            : null;
        if (!string.IsNullOrWhiteSpace(developmentUser))
        {
            return developmentUser;
        }

        if (authentication.PublicReadEnabled)
        {
            return authentication.PublicUserId;
        }

        throw new ApiAuthenticationException();
    }
}
