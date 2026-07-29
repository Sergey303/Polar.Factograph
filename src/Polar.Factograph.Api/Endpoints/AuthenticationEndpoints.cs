using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/auth/browser", () => Results.Ok(
            new BrowserAuthenticationConfiguration(false, null, null, null)));
        endpoints.MapGet("/api/auth/session", GetSession);
        endpoints.MapPost("/api/auth/register", Register);
        endpoints.MapPost("/api/auth/login", Login);
        endpoints.MapPost("/api/auth/logout", Logout).RequireAuthorization();
        endpoints.MapPost("/api/auth/logout-all", LogoutAll).RequireAuthorization();
        endpoints.MapPost("/api/auth/devices/{deviceId}/revoke", RevokeDevice)
            .RequireAuthorization();
        return endpoints;
    }

    private static IResult GetSession(
        HttpContext context,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        LocalAuthenticationOptions options)
    {
        AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? deviceId = context.User.FindFirstValue(LocalCookieValidator.DeviceClaim);
        LocalAuthenticationSession? session = userId is not null && deviceId is not null
            ? authentication.ResolveSession(userId, deviceId)
            : null;

        return Results.Ok(session is null
            ? new LocalSessionResponse(
                false,
                options.RegistrationEnabled,
                tokens.RequestToken!,
                null,
                Array.Empty<LocalDeviceResponse>())
            : new LocalSessionResponse(
                true,
                options.RegistrationEnabled,
                tokens.RequestToken!,
                new LocalUserResponse(
                    session.User.Id,
                    session.User.Login,
                    session.User.DisplayName,
                    session.User.Roles,
                    session.User.Fog?.CassetteId,
                    session.User.Fog?.DocumentUri),
                authentication.GetDevices(session.User.Id)
                    .Select(device => ToResponse(device, device.Id == session.Device.Id))
                    .ToArray()));
    }

    private static async Task<IResult> Register(
        HttpContext context,
        LocalRegisterRequest request,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        IResult? rejection = await ValidateAntiforgeryAsync(context, antiforgery);
        if (rejection is not null)
        {
            return rejection;
        }

        try
        {
            LocalAuthenticationSession session = await authentication.RegisterAsync(
                request.Login,
                request.Password,
                request.DisplayName,
                request.DeviceName,
                cancellationToken);
            await SignInAsync(context, session);
            return Results.Ok(ToAuthenticatedResponse(session));
        }
        catch (ArgumentException exception)
        {
            return Error(StatusCodes.Status400BadRequest, "invalid_registration", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Error(StatusCodes.Status409Conflict, "registration_unavailable", exception.Message);
        }
    }

    private static async Task<IResult> Login(
        HttpContext context,
        LocalLoginRequest request,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        IResult? rejection = await ValidateAntiforgeryAsync(context, antiforgery);
        if (rejection is not null)
        {
            return rejection;
        }

        LocalAuthenticationSession? session;
        try
        {
            session = await authentication.LoginAsync(
                request.Login,
                request.Password,
                request.DeviceName,
                cancellationToken);
        }
        catch (ArgumentException)
        {
            session = null;
        }

        if (session is null)
        {
            return Error(
                StatusCodes.Status401Unauthorized,
                "invalid_credentials",
                "Invalid login or password.");
        }

        await SignInAsync(context, session);
        return Results.Ok(ToAuthenticatedResponse(session));
    }

    private static async Task<IResult> Logout(
        HttpContext context,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        IResult? rejection = await ValidateAntiforgeryAsync(context, antiforgery);
        if (rejection is not null)
        {
            return rejection;
        }

        (string userId, string deviceId) = RequireSessionClaims(context);
        await authentication.RevokeDeviceAsync(userId, deviceId, cancellationToken);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAll(
        HttpContext context,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        IResult? rejection = await ValidateAntiforgeryAsync(context, antiforgery);
        if (rejection is not null)
        {
            return rejection;
        }

        (string userId, _) = RequireSessionClaims(context);
        await authentication.RevokeAllAsync(userId, cancellationToken);
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }

    private static async Task<IResult> RevokeDevice(
        string deviceId,
        HttpContext context,
        IAntiforgery antiforgery,
        LocalAuthenticationService authentication,
        CancellationToken cancellationToken)
    {
        IResult? rejection = await ValidateAntiforgeryAsync(context, antiforgery);
        if (rejection is not null)
        {
            return rejection;
        }

        (string userId, string currentDeviceId) = RequireSessionClaims(context);
        await authentication.RevokeDeviceAsync(userId, deviceId, cancellationToken);
        if (string.Equals(deviceId, currentDeviceId, StringComparison.Ordinal))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return Results.NoContent();
    }

    private static async Task SignInAsync(
        HttpContext context,
        LocalAuthenticationSession session)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, session.User.Id),
            new(ClaimTypes.Name, session.User.Login),
            new(LocalCookieValidator.DeviceClaim, session.Device.Id),
            new(
                LocalCookieValidator.SecurityVersionClaim,
                session.User.SecurityVersion.ToString(CultureInfo.InvariantCulture))
        ];
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = session.Device.ExpiresAtUtc
            });
    }

    private static LocalAuthenticatedResponse ToAuthenticatedResponse(
        LocalAuthenticationSession session) => new(
        session.User.Id,
        session.User.Login,
        session.User.DisplayName,
        session.User.Roles,
        session.User.Fog?.CassetteId,
        session.User.Fog?.DocumentUri,
        session.Device.Id,
        session.Device.ExpiresAtUtc);

    private static LocalDeviceResponse ToResponse(
        IdentityDevice device,
        bool current) => new(
        device.Id,
        device.Name,
        device.CreatedAtUtc,
        device.LastSeenAtUtc,
        device.ExpiresAtUtc,
        device.RevokedAtUtc,
        current);

    private static async Task<IResult?> ValidateAntiforgeryAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return null;
        }
        catch (AntiforgeryValidationException)
        {
            return Error(
                StatusCodes.Status400BadRequest,
                "antiforgery_failed",
                "The request verification token is missing or invalid.");
        }
    }

    private static (string UserId, string DeviceId) RequireSessionClaims(
        HttpContext context)
    {
        string userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new ApiAuthenticationException();
        string deviceId = context.User.FindFirstValue(LocalCookieValidator.DeviceClaim)
            ?? throw new ApiAuthenticationException();
        return (userId, deviceId);
    }

    private static IResult Error(int status, string code, string message) =>
        Results.Json(new ApiError(code, message), statusCode: status);
}

public sealed record BrowserAuthenticationConfiguration(
    bool Enabled,
    string? Authority,
    string? ClientId,
    string? Scope);

public sealed record LocalRegisterRequest(
    string Login,
    string Password,
    string? DisplayName,
    string? DeviceName);

public sealed record LocalLoginRequest(
    string Login,
    string Password,
    string? DeviceName);

public sealed record LocalSessionResponse(
    bool Authenticated,
    bool RegistrationEnabled,
    string AntiforgeryToken,
    LocalUserResponse? User,
    LocalDeviceResponse[] Devices);

public sealed record LocalUserResponse(
    string Id,
    string Login,
    string DisplayName,
    string[] Roles,
    string? FogCassetteId,
    string? FogDocumentUri);

public sealed record LocalDeviceResponse(
    string Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastSeenAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc,
    bool Current);

public sealed record LocalAuthenticatedResponse(
    string UserId,
    string Login,
    string DisplayName,
    string[] Roles,
    string? FogCassetteId,
    string? FogDocumentUri,
    string DeviceId,
    DateTimeOffset ExpiresAtUtc);
