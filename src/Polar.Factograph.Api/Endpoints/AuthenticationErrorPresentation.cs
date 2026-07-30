using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Endpoints;

public sealed record AuthenticationFailureResponse(
    int StatusCode,
    ApiError Error);

public static class AuthenticationErrorPresentation
{
    private const string RegistrationUnavailableMessage =
        "Регистрация временно недоступна. Повторите попытку позже.";

    public static AuthenticationFailureResponse RegistrationUnavailable(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return new AuthenticationFailureResponse(
            StatusCodes.Status409Conflict,
            new ApiError(
                "registration_unavailable",
                RegistrationUnavailableMessage));
    }
}
