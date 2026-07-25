using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public async Task<ProjectDefinition> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        await using FileStream stream = File.OpenRead(fullPath);
        ProjectDefinition project = await JsonSerializer.DeserializeAsync<ProjectDefinition>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidDataException("Project configuration is empty.");

        project = ResolvePaths(project, Path.GetDirectoryName(fullPath)!);
        Validate(project);
        return project;
    }

    private static ProjectDefinition ResolvePaths(ProjectDefinition project, string baseDirectory)
    {
        return project with
        {
            Ontology = project.Ontology with
            {
                Path = ResolvePath(baseDirectory, project.Ontology.Path)
            },
            Index = project.Index with
            {
                Path = ResolvePath(baseDirectory, project.Index.Path)
            },
            Cassettes = project.Cassettes
                .Select(cassette => cassette with
                {
                    Path = ResolvePath(baseDirectory, cassette.Path)
                })
                .ToArray()
        };
    }

    private static string ResolvePath(string baseDirectory, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path) || IsWindowsDrivePath(path))
        {
            return path;
        }

        return Path.GetFullPath(path, baseDirectory);
    }

    private static bool IsWindowsDrivePath(string path)
    {
        return path.Length >= 3 &&
               char.IsLetter(path[0]) &&
               path[1] == ':' &&
               (path[2] == '/' || path[2] == '\\');
    }

    private static void Validate(ProjectDefinition project)
    {
        if (project.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported project schema version: {project.SchemaVersion}.");
        }

        if (string.IsNullOrWhiteSpace(project.ProjectId))
        {
            throw new InvalidDataException("ProjectId is required.");
        }

        string? duplicateCassette = project.Cassettes
            .GroupBy(cassette => cassette.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateCassette is not null)
        {
            throw new InvalidDataException($"Duplicate cassette id: {duplicateCassette}.");
        }

        foreach (MemberDefinition member in project.Members)
        {
            foreach (string role in member.Roles)
            {
                if (!project.Roles.ContainsKey(role))
                {
                    throw new InvalidDataException($"Unknown role '{role}' for user '{member.UserId}'.");
                }
            }
        }
    }
}
