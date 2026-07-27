using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api.Authentication;

public sealed class AuthenticationStorageExceptionMiddleware(
    RequestDelegate next,
    ILogger<AuthenticationStorageExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (IOException exception) when (
            context.Request.Path.StartsWithSegments("/api/auth") &&
            !context.Response.HasStarted)
        {
            logger.LogError(exception, "Failed to update local identity storage.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new ApiError(
                    "identity_storage_unavailable",
                    "Не удалось сохранить данные пользователя. Повторите попытку."),
                context.RequestAborted);
        }
    }
}
