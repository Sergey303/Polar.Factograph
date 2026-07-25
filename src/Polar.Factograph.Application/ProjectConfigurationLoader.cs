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

        await using FileStream stream = File.OpenRead(path);
        ProjectDefinition project = await JsonSerializer.DeserializeAsync<ProjectDefinition>(
            stream,
            JsonOptions,
            cancellationToken)
            ?? throw new InvalidDataException("Project configuration is empty.");

        Validate(project);
        return project;
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
