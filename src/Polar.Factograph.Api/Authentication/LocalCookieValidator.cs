using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Polar.Factograph.Api.Authentication;

public sealed class LocalCookieValidator(
    LocalAuthenticationService authentication) : CookieAuthenticationEvents
{
    public const string DeviceClaim = "device_id";
    public const string SecurityVersionClaim = "security_version";

    public override async Task ValidatePrincipal(
        CookieValidatePrincipalContext context)
    {
        string? userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        string? deviceId = context.Principal?.FindFirstValue(DeviceClaim);
        string? versionText = context.Principal?.FindFirstValue(SecurityVersionClaim);
        LocalAuthenticationSession? session = userId is not null && deviceId is not null
            ? authentication.ResolveSession(userId, deviceId)
            : null;

        bool validVersion = int.TryParse(
            versionText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int securityVersion);
        if (session is not null &&
            validVersion &&
            session.User.SecurityVersion == securityVersion)
        {
            return;
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public override Task RedirectToLogin(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(
        RedirectContext<CookieAuthenticationOptions> context)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
