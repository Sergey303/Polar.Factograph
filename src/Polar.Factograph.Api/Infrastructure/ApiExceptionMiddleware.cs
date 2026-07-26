using Polar.Factograph.Application;

namespace Polar.Factograph.Api.Infrastructure;

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
            await ApiErrorWriter.WriteAsync(
                context, 401, "authentication_required", exception.Message);
        }
        catch (UnauthorizedAccessException exception) when (
            exception is ProjectAuthorizationException or CassetteAuthorizationException)
        {
            await ApiErrorWriter.WriteAsync(context, 403, "forbidden", exception.Message);
        }
        catch (ProjectWriteCommittedException exception)
        {
            logger.LogError(exception, "Fog write committed but index refresh failed.");
            await ApiErrorWriter.WriteAsync(
                context,
                503,
                "write_committed_index_refresh_failed",
                exception.Message);
        }
        catch (ProjectRuntimeUnavailableException exception)
        {
            await ApiErrorWriter.WriteAsync(
                context, 503, "project_unavailable", exception.Message);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or ArgumentException)
        {
            await ApiErrorWriter.WriteAsync(
                context, 400, "invalid_request", exception.Message);
        }
        catch (IOException exception)
        {
            await ApiErrorWriter.WriteAsync(
                context, 503, "storage_unavailable", exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled API request failure.");
            await ApiErrorWriter.WriteAsync(
                context,
                500,
                "internal_error",
                "An unexpected server error occurred.");
        }
    }
}
