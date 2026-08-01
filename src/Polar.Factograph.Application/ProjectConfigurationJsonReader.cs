using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectConfigurationJsonReader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static async Task<ProjectDefinition> ReadAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = File.OpenRead(fullPath);
            using JsonDocument document = await JsonDocument.ParseAsync(
                stream,
                DocumentOptions,
                cancellationToken);
            RejectRemovedAccessSections(document.RootElement);

            ProjectDefinition project = document.RootElement.Deserialize<ProjectDefinition>(JsonOptions)
                ?? throw new InvalidDataException("Project configuration is empty.");
            return ProjectBuiltInAccess.Apply(project);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Project configuration JSON cannot be read: {fullPath}",
                exception);
        }
    }

    private static void RejectRemovedAccessSections(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Project configuration root must be an object.");
        }

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (property.Name.Equals("roles", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("members", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("writeRouting", StringComparison.OrdinalIgnoreCase))
            {
                throw new JsonException(
                    $"Project section '{property.Name}' is no longer supported. " +
                    "Viewer, editor, and administrator access is built in.");
            }
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
