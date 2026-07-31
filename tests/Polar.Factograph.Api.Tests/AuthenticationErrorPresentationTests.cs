using Microsoft.AspNetCore.Http;
using Polar.Factograph.Api.Endpoints;

namespace Polar.Factograph.Api.Tests;

public sealed class AuthenticationErrorPresentationTests
{
    [Fact]
    public void RegistrationUnavailable_HidesProjectConfigurationDetails()
    {
        AuthenticationFailureResponse response =
            AuthenticationErrorPresentation.RegistrationUnavailable(
                new InvalidOperationException(
                    "Роль 'editor' отсутствует; кассета 'private-cassette' недоступна."));

        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);
        Assert.Equal("registration_unavailable", response.Error.Code);
        Assert.Equal(
            "Регистрация временно недоступна. Повторите попытку позже.",
            response.Error.Message);
        Assert.DoesNotContain("editor", response.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private-cassette", response.Error.Message, StringComparison.Ordinal);
    }
}
