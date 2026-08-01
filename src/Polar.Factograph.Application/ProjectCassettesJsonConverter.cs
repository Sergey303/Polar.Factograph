using System.Text.Json;
using System.Text.Json.Serialization;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal sealed class ProjectCassettesJsonConverter : JsonConverter<CassetteDefinition[]>
{
    public override CassetteDefinition[] Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException(
                "Project 'cassettes' must be an object with 'items' and 'write' fields.");
        }

        JsonElement? itemsElement = null;
        string? writePath = null;
        HashSet<string> fields = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!fields.Add(property.Name))
            {
                throw new JsonException(
                    $"Project 'cassettes' contains duplicate field '{property.Name}'.");
            }

            if (string.Equals(property.Name, "items", StringComparison.OrdinalIgnoreCase))
            {
                itemsElement = property.Value;
            }
            else if (string.Equals(property.Name, "write", StringComparison.OrdinalIgnoreCase))
            {
                writePath = ReadRequiredString(property.Value, "cassettes.write");
            }
            else
            {
                throw new JsonException(
                    $"Unknown project 'cassettes' field '{property.Name}'. Only 'items' and 'write' are supported.");
            }
        }

        if (itemsElement is null || itemsElement.Value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Project 'cassettes.items' must be an array of full paths.");
        }

        if (writePath is null)
        {
            throw new JsonException("Project 'cassettes.write' full path is required.");
        }

        string normalizedWritePath = NormalizePath(writePath, "cassettes.write");
        List<string> paths = [];
        HashSet<string> normalizedPaths = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> cassetteIds = new(StringComparer.OrdinalIgnoreCase);

        int index = 0;
        foreach (JsonElement value in itemsElement.Value.EnumerateArray())
        {
            string path = ReadRequiredString(value, $"cassettes.items[{index}]");
            string normalizedPath = NormalizePath(path, $"cassettes.items[{index}]");
            string cassetteId = GetCassetteId(normalizedPath, $"cassettes.items[{index}]");

            if (!normalizedPaths.Add(normalizedPath))
            {
                throw new JsonException($"Duplicate cassette path: {path}");
            }

            if (!cassetteIds.Add(cassetteId))
            {
                throw new JsonException(
                    $"Cassette folder name must be unique: {cassetteId}");
            }

            paths.Add(normalizedPath);
            index++;
        }

        if (paths.Count == 0)
        {
            throw new JsonException("Project 'cassettes.items' must contain at least one path.");
        }

        if (!normalizedPaths.Contains(normalizedWritePath))
        {
            throw new JsonException(
                "Project 'cassettes.write' must exactly match one path from 'cassettes.items'.");
        }

        return paths.Select(path =>
        {
            string cassetteId = GetCassetteId(path, "cassettes.items");
            return new CassetteDefinition
            {
                Id = cassetteId,
                Name = cassetteId,
                Path = path,
                Enabled = true,
                DefaultAccess = ProjectRights.Read,
                AllowWrite = string.Equals(
                    path,
                    normalizedWritePath,
                    StringComparison.OrdinalIgnoreCase)
            };
        }).ToArray();
    }

    public override void Write(
        Utf8JsonWriter writer,
        CassetteDefinition[] value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(value);
        CassetteDefinition[] writable = value.Where(cassette => cassette.AllowWrite).ToArray();
        if (writable.Length != 1)
        {
            throw new JsonException(
                "Exactly one cassette must be marked writable before serializing project configuration.");
        }

        writer.WriteStartObject();
        writer.WritePropertyName("items");
        writer.WriteStartArray();
        foreach (CassetteDefinition cassette in value)
        {
            writer.WriteStringValue(cassette.Path);
        }
        writer.WriteEndArray();
        writer.WriteString("write", writable[0].Path);
        writer.WriteEndObject();
    }

    private static string ReadRequiredString(JsonElement value, string fieldName)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Project '{fieldName}' must be a string.");
        }

        string? text = value.GetString()?.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? throw new JsonException($"Project '{fieldName}' cannot be empty.")
            : text;
    }

    private static string NormalizePath(string path, string fieldName)
    {
        string normalized = path.Trim().TrimEnd('/', '\\');
        if (normalized.Length == 0 || !IsFullPath(normalized))
        {
            throw new JsonException(
                $"Project '{fieldName}' must contain a full cassette path: {path}");
        }

        return normalized.Replace('\\', '/');
    }

    private static bool IsFullPath(string path) =>
        Path.IsPathRooted(path) ||
        (path.Length >= 3 &&
         char.IsLetter(path[0]) &&
         path[1] == ':' &&
         (path[2] == '/' || path[2] == '\\'));

    private static string GetCassetteId(string path, string fieldName)
    {
        int separator = path.LastIndexOfAny(['/', '\\']);
        string id = separator >= 0 ? path[(separator + 1)..] : path;
        return string.IsNullOrWhiteSpace(id)
            ? throw new JsonException(
                $"Project '{fieldName}' path has no cassette folder name: {path}")
            : id;
    }
}
