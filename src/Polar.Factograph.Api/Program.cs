using Polar.Factograph.Application;
using Polar.Factograph.Domain;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ProjectConfigurationLoader>();

WebApplication app = builder.Build();

app.MapGet("/api/system/health", () => Results.Ok(new
{
    service = "Polar.Factograph.Api",
    status = "ok"
}));

app.MapGet("/api/project", async (
    ProjectConfigurationLoader loader,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    string? configuredPath = configuration["Project:ConfigPath"];
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return Results.NotFound(new { error = "Project:ConfigPath is not configured." });
    }

    string path = Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());

    if (!File.Exists(path))
    {
        return Results.NotFound(new { error = $"Project configuration was not found: {path}" });
    }

    try
    {
        ProjectDefinition project = await loader.LoadAsync(path, cancellationToken);
        return Results.Ok(project);
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/", () => Results.Redirect("/api/system/health"));

app.Run();

public partial class Program
{
}
