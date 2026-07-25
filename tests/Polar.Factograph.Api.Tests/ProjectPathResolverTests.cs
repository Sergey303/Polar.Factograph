using Microsoft.Extensions.Configuration;
using Polar.Factograph.Api.Infrastructure;
using Xunit;

namespace Polar.Factograph.Api.Tests;

public sealed class ProjectPathResolverTests
{
    [Fact]
    public void GetRequiredPath_ResolvesFromContentRoot()
    {
        string directory = CreateDirectory();
        try
        {
            string expected = Path.Combine(directory, "config", "project.json");
            Directory.CreateDirectory(Path.GetDirectoryName(expected)!);
            File.WriteAllText(expected, "{}");
            ProjectPathResolver resolver = CreateResolver(directory, "config/project.json");

            Assert.Equal(expected, resolver.GetRequiredPath());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void GetRequiredPath_RejectsMissingConfigurationFile()
    {
        string directory = CreateDirectory();
        try
        {
            ProjectPathResolver resolver = CreateResolver(directory, "missing.json");

            Assert.Throws<ProjectRuntimeUnavailableException>(resolver.GetRequiredPath);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static ProjectPathResolver CreateResolver(string contentRoot, string path)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Project:ConfigPath"] = path
            })
            .Build();
        TestHostEnvironment environment = new() { ContentRootPath = contentRoot };
        return new ProjectPathResolver(configuration, environment);
    }

    private static string CreateDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-api-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
