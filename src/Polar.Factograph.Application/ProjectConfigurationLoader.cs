using System.Text.Json;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectConfigurationLoader
{
    private const string NoDefaultAccess = "none";
    private const string Wildcard = "*";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly HashSet<string> KnownProjectRights = new(StringComparer.Ordinal)
    {
        ProjectRights.Read,
        ProjectRights.Search,
        ProjectRights.Export,
        ProjectRights.ManageUsers,
        ProjectRights.ManageCassettes,
        ProjectRights.RebuildIndex
    };

    private static readonly HashSet<string> KnownCassetteRights = new(StringComparer.Ordinal)
    {
        CassetteRights.Read,
        CassetteRights.WriteMetadata,
        CassetteRights.AddDocuments,
        CassetteRights.ReplaceDocuments,
        CassetteRights.Delete,
        CassetteRights.Substitute,
        CassetteRights.Manage
    };

    private static readonly HashSet<string> WriteRights = new(StringComparer.Ordinal)
    {
        CassetteRights.WriteMetadata,
        CassetteRights.AddDocuments,
        CassetteRights.ReplaceDocuments,
        CassetteRights.Delete,
        CassetteRights.Substitute,
        CassetteRights.Manage
    };

    public async Task<ProjectDefinition> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        ProjectDefinition project;
        try
        {
            await using FileStream stream = File.OpenRead(fullPath);
            project = await JsonSerializer.DeserializeAsync<ProjectDefinition>(
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

        ValidateRequiredShape(project);
        project = ResolvePaths(project, Path.GetDirectoryName(fullPath)!);
        ValidateReferencesAndRights(project);
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
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidDataException("A configured filesystem path is empty.");
        }

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

    private static void ValidateRequiredShape(ProjectDefinition project)
    {
        if (project.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported project schema version: {project.SchemaVersion}.");
        }

        RequireText(project.ProjectId, "ProjectId is required.");
        RequireText(project.Name, "Project name is required.");

        if (project.Ontology is null)
        {
            throw new InvalidDataException("Ontology configuration is required.");
        }

        if (project.Index is null)
        {
            throw new InvalidDataException("Index configuration is required.");
        }

        RequireText(project.Ontology.Path, "Ontology path is required.");
        RequireText(project.Index.Path, "Index path is required.");

        foreach (CassetteDefinition cassette in project.Cassettes)
        {
            RequireText(cassette.Id, "Cassette id is required.");
            RequireText(cassette.Name, $"Cassette name is required for '{cassette.Id}'.");
            RequireText(cassette.Path, $"Cassette path is required for '{cassette.Id}'.");
            if (!string.Equals(cassette.DefaultAccess, NoDefaultAccess, StringComparison.Ordinal) &&
                !string.Equals(cassette.DefaultAccess, ProjectRights.Read, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Unknown defaultAccess '{cassette.DefaultAccess}' for cassette '{cassette.Id}'.");
            }
        }

        foreach ((string roleName, RoleDefinition role) in project.Roles)
        {
            RequireText(roleName, "Role name is required.");
            ArgumentNullException.ThrowIfNull(role);
        }

        foreach (MemberDefinition member in project.Members)
        {
            RequireText(member.UserId, "Member userId is required.");
        }
    }

    private static void ValidateReferencesAndRights(ProjectDefinition project)
    {
        HashSet<string> cassetteIds = project.Cassettes
            .Select(cassette => cassette.Id)
            .ToHashSet(StringComparer.Ordinal);

        string? duplicateCassette = project.Cassettes
            .GroupBy(cassette => cassette.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateCassette is not null)
        {
            throw new InvalidDataException($"Duplicate cassette id: {duplicateCassette}.");
        }

        string? duplicateMember = project.Members
            .GroupBy(member => member.UserId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicateMember is not null)
        {
            throw new InvalidDataException($"Duplicate project member: {duplicateMember}.");
        }

        foreach ((string roleName, RoleDefinition role) in project.Roles)
        {
            ValidateRights(role.ProjectRights, KnownProjectRights, $"role '{roleName}' project rights");
            ValidateCassetteRightsMap(
                role.CassetteRights,
                cassetteIds,
                $"role '{roleName}' cassette rights");
        }

        foreach (MemberDefinition member in project.Members)
        {
            ValidateNoDuplicates(member.Roles, $"roles for user '{member.UserId}'");
            foreach (string roleName in member.Roles)
            {
                if (!project.Roles.ContainsKey(roleName))
                {
                    throw new InvalidDataException(
                        $"Unknown role '{roleName}' for user '{member.UserId}'.");
                }
            }

            ValidateCassetteRightsMap(
                member.CassetteOverrides,
                cassetteIds,
                $"cassette overrides for user '{member.UserId}'");
        }

        foreach ((string roleName, string cassetteId) in project.WriteRouting.DefaultCassetteByRole)
        {
            if (!project.Roles.TryGetValue(roleName, out RoleDefinition? role))
            {
                throw new InvalidDataException(
                    $"Write routing references unknown role '{roleName}'.");
            }

            CassetteDefinition? cassette = project.Cassettes.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, cassetteId, StringComparison.Ordinal));
            if (cassette is null)
            {
                throw new InvalidDataException(
                    $"Write routing for role '{roleName}' references unknown cassette '{cassetteId}'.");
            }

            if (!cassette.Enabled || !cassette.AllowWrite)
            {
                throw new InvalidDataException(
                    $"Write routing for role '{roleName}' targets non-writable cassette '{cassetteId}'.");
            }

            if (!RoleHasWriteRight(role, cassetteId))
            {
                throw new InvalidDataException(
                    $"Role '{roleName}' has no write right for routed cassette '{cassetteId}'.");
            }
        }
    }

    private static bool RoleHasWriteRight(RoleDefinition role, string cassetteId)
    {
        IEnumerable<string> rights = Enumerable.Empty<string>();
        if (role.CassetteRights.TryGetValue(Wildcard, out string[]? wildcard))
        {
            rights = rights.Concat(wildcard);
        }

        if (role.CassetteRights.TryGetValue(cassetteId, out string[]? exact))
        {
            rights = rights.Concat(exact);
        }

        return rights.Any(WriteRights.Contains);
    }

    private static void ValidateCassetteRightsMap(
        IReadOnlyDictionary<string, string[]> configured,
        IReadOnlySet<string> cassetteIds,
        string location)
    {
        foreach ((string cassetteId, string[] rights) in configured)
        {
            if (!string.Equals(cassetteId, Wildcard, StringComparison.Ordinal) &&
                !cassetteIds.Contains(cassetteId))
            {
                throw new InvalidDataException(
                    $"Unknown cassette '{cassetteId}' in {location}.");
            }

            ValidateRights(rights, KnownCassetteRights, $"{location} for '{cassetteId}'");
        }
    }

    private static void ValidateRights(
        IReadOnlyList<string> rights,
        IReadOnlySet<string> knownRights,
        string location)
    {
        ValidateNoDuplicates(rights, location);
        foreach (string right in rights)
        {
            if (!knownRights.Contains(right))
            {
                throw new InvalidDataException($"Unknown right '{right}' in {location}.");
            }
        }
    }

    private static void ValidateNoDuplicates(
        IReadOnlyList<string> values,
        string location)
    {
        string? duplicate = values
            .GroupBy(value => value, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate value '{duplicate}' in {location}.");
        }
    }

    private static void RequireText(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException(message);
        }
    }
}