namespace Polar.Factograph.Api.Infrastructure;

public sealed class ApiAuthenticationException : UnauthorizedAccessException
{
    public ApiAuthenticationException()
        : base("An authenticated project user is required.")
    {
    }
}

public sealed class ProjectRuntimeUnavailableException : InvalidOperationException
{
    public ProjectRuntimeUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
