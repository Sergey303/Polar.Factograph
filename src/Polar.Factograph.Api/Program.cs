using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ProjectConfigurationLoader>();
builder.Services.AddSingleton<IFogSourceScanner, FileSystemFogSourceScanner>();

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
    string? projectPath = ResolveProjectPath(configuration);
    if (projectPath is null)
    {
        return Results.NotFound(new { error = "Project:ConfigPath is not configured." });
    }

    if (!File.Exists(projectPath))
    {
        return Results.NotFound(new { error = $"Project configuration was not found: {projectPath}" });
    }

    try
    {
        ProjectDefinition project = await loader.LoadAsync(projectPath, cancellationToken);
        return Results.Ok(project);
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/api/project/sources", async (
    ProjectConfigurationLoader loader,
    IFogSourceScanner scanner,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    string? projectPath = ResolveProjectPath(configuration);
    if (projectPath is null)
    {
        return Results.NotFound(new { error = "Project:ConfigPath is not configured." });
    }

    if (!File.Exists(projectPath))
    {
        return Results.NotFound(new { error = $"Project configuration was not found: {projectPath}" });
    }

    try
    {
        ProjectDefinition project = await loader.LoadAsync(projectPath, cancellationToken);
        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(project, cancellationToken);
        return Results.Ok(sources);
    }
    catch (Exception exception) when (
        exception is InvalidDataException or
        DirectoryNotFoundException or
        FileNotFoundException)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapGet("/", () => Results.Redirect("/api/system/health"));

app.Run();

static string? ResolveProjectPath(IConfiguration configuration)
{
    string? configuredPath = configuration["Project:ConfigPath"];
    if (string.IsNullOrWhiteSpace(configuredPath))
    {
        return null;
    }

    return Path.IsPathRooted(configuredPath)
        ? configuredPath
        : Path.GetFullPath(configuredPath, Directory.GetCurrentDirectory());
}

public partial class Program
{
}
