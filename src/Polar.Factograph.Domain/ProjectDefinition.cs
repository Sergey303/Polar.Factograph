namespace Polar.Factograph.Domain;

public sealed record ProjectDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public required string ProjectId { get; init; }
    public required string Name { get; init; }
    public required OntologyDefinition Ontology { get; init; }
    public required IndexDefinition Index { get; init; }
    public string[] HomeResourceIds { get; init; } = Array.Empty<string>();
    public CassetteDefinition[] Cassettes { get; init; } = Array.Empty<CassetteDefinition>();
    public Dictionary<string, RoleDefinition> Roles { get; init; } = new(StringComparer.Ordinal);
    public MemberDefinition[] Members { get; init; } = Array.Empty<MemberDefinition>();
}

public sealed record OntologyDefinition
{
    public required string Path { get; init; }
}

public sealed record IndexDefinition
{
    public required string Path { get; init; }
    public string RebuildMode { get; init; } = "whenSourcesChanged";
}

public sealed record CassetteDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public bool Enabled { get; init; } = true;
    public string DefaultAccess { get; init; } = "read";
    public bool AllowWrite { get; init; }
}

public sealed record RoleDefinition
{
    public string[] ProjectRights { get; init; } = Array.Empty<string>();
    public Dictionary<string, string[]> CassetteRights { get; init; } = new(StringComparer.Ordinal);
}

public sealed record MemberDefinition
{
    public required string UserId { get; init; }
    public string[] Roles { get; init; } = Array.Empty<string>();
    public Dictionary<string, string[]> CassetteOverrides { get; init; } = new(StringComparer.Ordinal);
}

public static class ProjectRights
{
    public const string Read = "read";
    public const string Search = "search";
    public const string Export = "export";
    public const string ManageUsers = "manageUsers";
    public const string ManageCassettes = "manageCassettes";
    public const string RebuildIndex = "rebuildIndex";
}

public static class CassetteRights
{
    public const string Read = "read";
    public const string WriteMetadata = "writeMetadata";
    public const string AddDocuments = "addDocuments";
    public const string ReplaceDocuments = "replaceDocuments";
    public const string Delete = "delete";
    public const string Substitute = "substitute";
    public const string Manage = "manage";
}
