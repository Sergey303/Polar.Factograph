using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ApiError(string Code, string Message);

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    private const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = ClientClosedRequest;
        }
        catch (ApiAuthenticationException exception)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, "authentication_required", exception.Message);
        }
        catch (ProjectAuthorizationException exception)
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, "forbidden", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            await WriteAsync(context, StatusCodes.Status403Forbidden, "forbidden", exception.Message);
        }
        catch (ProjectRuntimeUnavailableException exception)
        {
            await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "project_unavailable", exception.Message);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            await WriteAsync(context, StatusCodes.Status400BadRequest, "invalid_request", exception.Message);
        }
        catch (IOException exception) when (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            logger.LogError(exception, "Failed to update local identity storage.");
            await WriteAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "identity_storage_unavailable",
                "Не удалось сохранить данные пользователя. Повторите попытку.");
        }
        catch (IOException exception)
        {
            await WriteAsync(context, StatusCodes.Status503ServiceUnavailable, "storage_unavailable", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API request failure.");
            await WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "internal_error",
                "An unexpected server error occurred.");
        }
    }

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ApiError(code, message),
            context.RequestAborted);
    }
}
