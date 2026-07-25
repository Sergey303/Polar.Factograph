namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectPathResolver(
    IConfiguration configuration,
    IHostEnvironment environment)
{
    public string GetRequiredPath()
    {
        string? configuredPath = configuration["Project:ConfigPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new ProjectRuntimeUnavailableException(
                "Project:ConfigPath is not configured.");
        }

        string fullPath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(configuredPath, environment.ContentRootPath);

        if (!File.Exists(fullPath))
        {
            throw new ProjectRuntimeUnavailableException(
                $"Project configuration was not found: {fullPath}");
        }

        return fullPath;
    }
}
