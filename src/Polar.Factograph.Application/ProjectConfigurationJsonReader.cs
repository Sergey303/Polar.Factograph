using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectConfigurationJsonReader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static async Task<ProjectDefinition> ReadAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = File.OpenRead(fullPath);
            return await JsonSerializer.DeserializeAsync<ProjectDefinition>(
                stream,
                JsonOptions,
                cancellationToken)
                ?? throw new InvalidDataException("Project configuration is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Project configuration JSON cannot be read: {fullPath}",
                exception);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };
        options.Converters.Add(new ProjectCassettesJsonConverter());
        return options;
    }
}
