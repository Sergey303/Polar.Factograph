using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Infrastructure;

public sealed record ApiError(string Code, string Message);

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    private const int ClientClosedRequest = 499;
    private const string ForbiddenMessage = "Недостаточно прав для выполнения операции.";
    private const string ProjectUnavailableMessage = "Проект временно недоступен. Повторите попытку позже.";
    private const string StorageUnavailableMessage = "Хранилище временно недоступно. Повторите попытку позже.";

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
            logger.LogWarning(
                exception,
                "Project authorization denied for user {UserId}; required right {RequiredRight}.",
                exception.UserId,
                exception.RequiredRight);
            await WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "forbidden",
                ForbiddenMessage);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning(exception, "Authorization denied by an API operation.");
            await WriteAsync(
                context,
                StatusCodes.Status403Forbidden,
                "forbidden",
                ForbiddenMessage);
        }
        catch (ProjectRuntimeUnavailableException exception)
        {
            logger.LogError(exception, "Project runtime is unavailable.");
            await WriteAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "project_unavailable",
                ProjectUnavailableMessage);
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
            logger.LogError(exception, "Storage operation failed.");
            await WriteAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "storage_unavailable",
                StorageUnavailableMessage);
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
