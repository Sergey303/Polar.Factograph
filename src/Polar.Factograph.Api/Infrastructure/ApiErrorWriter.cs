namespace Polar.Factograph.Api.Infrastructure;

public sealed record ApiError(string Code, string Message);

internal static class ApiErrorWriter
{
    public static async Task WriteAsync(
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
