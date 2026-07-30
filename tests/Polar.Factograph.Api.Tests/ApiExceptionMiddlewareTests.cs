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
        ApiError response = await InvokeAsync(new ProjectAuthorizationException(
            "public-reader",
            "writeMetadata"));

        Assert.Equal("forbidden", response.Code);
        Assert.Equal("Недостаточно прав для выполнения операции.", response.Message);
        Assert.DoesNotContain("public-reader", response.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("writeMetadata", response.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_HidesGenericUnauthorizedMessage()
    {
        ApiError response = await InvokeAsync(new UnauthorizedAccessException(
            "Secret cassette path and role details."));

        Assert.Equal("forbidden", response.Code);
        Assert.Equal("Недостаточно прав для выполнения операции.", response.Message);
        Assert.DoesNotContain("Secret", response.Message, StringComparison.Ordinal);
    }

    private static async Task<ApiError> InvokeAsync(Exception exception)
    {
        ApiExceptionMiddleware middleware = new(
            _ => Task.FromException(exception),
            NullLogger<ApiExceptionMiddleware>.Instance);
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        ApiError? response = await JsonSerializer.DeserializeAsync<ApiError>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Assert.IsType<ApiError>(response);
    }
}
