using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_HidesProjectAuthorizationDetails()
    {
        ApiError response = await InvokeAsync(
            new ProjectAuthorizationException("public-reader", "writeMetadata"),
            StatusCodes.Status403Forbidden);

        Assert.Equal("forbidden", response.Code);
        Assert.Equal("Недостаточно прав для выполнения операции.", response.Message);
        Assert.DoesNotContain("public-reader", response.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("writeMetadata", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_HidesGenericUnauthorizedMessage()
    {
        ApiError response = await InvokeAsync(
            new UnauthorizedAccessException("Secret cassette path and role details."),
            StatusCodes.Status403Forbidden);

        Assert.Equal("forbidden", response.Code);
        Assert.Equal("Недостаточно прав для выполнения операции.", response.Message);
        Assert.DoesNotContain("Secret", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_HidesProjectRuntimeDetails()
    {
        ApiError response = await InvokeAsync(
            new ProjectRuntimeUnavailableException(
                "CURRENT points to D:\\projects\\secret-index\\generation-42."),
            StatusCodes.Status503ServiceUnavailable);

        Assert.Equal("project_unavailable", response.Code);
        Assert.Equal("Проект временно недоступен. Повторите попытку позже.", response.Message);
        Assert.DoesNotContain("D:\\projects", response.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("generation-42", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_HidesStoragePathFromIoException()
    {
        ApiError response = await InvokeAsync(
            new IOException("Cannot access C:\\secret\\cassette\\meta.fog."),
            StatusCodes.Status503ServiceUnavailable);

        Assert.Equal("storage_unavailable", response.Code);
        Assert.Equal("Хранилище временно недоступно. Повторите попытку позже.", response.Message);
        Assert.DoesNotContain("C:\\secret", response.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("meta.fog", response.Message, StringComparison.Ordinal);
    }

    private static async Task<ApiError> InvokeAsync(Exception exception, int expectedStatus)
    {
        ApiExceptionMiddleware middleware = new(
            _ => Task.FromException(exception),
            NullLogger<ApiExceptionMiddleware>.Instance);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        ApiError? response = await JsonSerializer.DeserializeAsync<ApiError>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Assert.IsType<ApiError>(response);
    }
}
