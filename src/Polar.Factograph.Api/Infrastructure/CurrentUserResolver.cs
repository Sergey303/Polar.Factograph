using System.Security.Claims;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class CurrentUserResolver(
    IConfiguration configuration,
    IHostEnvironment environment)
{
    public string Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        string? developmentUser = environment.IsDevelopment()
            ? configuration["Api:DevelopmentUserId"]
            : null;

        return !string.IsNullOrWhiteSpace(developmentUser)
            ? developmentUser
            : throw new ApiAuthenticationException();
    }
}
