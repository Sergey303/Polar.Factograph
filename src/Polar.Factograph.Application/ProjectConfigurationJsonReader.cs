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
            // ReadAllTextAsync detects and removes a UTF-8 BOM before JSON parsing.
            // Some existing project files and test fixtures are intentionally written
            // with that signature, so parsing raw bytes would reject otherwise valid JSON.
            string json = await File.ReadAllTextAsync(fullPath, cancellationToken);
            using JsonDocument document = JsonDocument.Parse(json, DocumentOptions);
            RejectRemovedHomeResourceList(document.RootElement);

            ProjectDefinition project = JsonSerializer.Deserialize<ProjectDefinition>(
                    json,
                    JsonOptions)
                ?? throw new InvalidDataException("Project configuration is empty.");

            // Access is intentionally fixed by the application. Legacy roles, members,
            // and write-routing values are overwritten during migration.
            return ProjectBuiltInAccess.Apply(project);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                $"Project configuration JSON cannot be read: {fullPath}",
                exception);
        }
    }

    private static void RejectRemovedHomeResourceList(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        bool hasRemovedField = root.EnumerateObject().Any(property =>
            string.Equals(
                property.Name,
                "homeResourceIds",
                StringComparison.OrdinalIgnoreCase));
        if (hasRemovedField)
        {
            throw new InvalidDataException(
                "Project configuration field 'homeResourceIds' was removed. " +
                "Use one string field 'homeResourceId' instead.");
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
