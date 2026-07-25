using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Polar.Factograph.Api.Tests;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "Polar.Factograph.Api.Tests";

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
