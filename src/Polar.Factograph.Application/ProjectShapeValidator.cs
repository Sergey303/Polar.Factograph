using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectShapeValidator
{
    public static void Validate(ProjectDefinition project)
    {
        if (project.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported project schema version: {project.SchemaVersion}.");
        }

        ProjectConfigurationValidation.RequireText(project.ProjectId, "ProjectId is required.");
        ProjectConfigurationValidation.RequireText(project.Name, "Project name is required.");

        if (project.Ontology is null)
        {
            throw new InvalidDataException("Ontology configuration is required.");
        }

        if (project.Index is null)
        {
            throw new InvalidDataException("Index configuration is required.");
        }

        ProjectConfigurationValidation.RequireText(
            project.Ontology.Path,
            "Ontology path is required.");
        ProjectConfigurationValidation.RequireText(project.Index.Path, "Index path is required.");

        if (project.Cassettes.Length == 0)
        {
            throw new InvalidDataException("At least one cassette is required.");
        }

        foreach (CassetteDefinition cassette in project.Cassettes)
        {
            ValidateCassette(cassette);
        }

        int writableCount = project.Cassettes.Count(cassette =>
            cassette.Enabled && cassette.AllowWrite);
        if (writableCount != 1)
        {
            throw new InvalidDataException(
                $"Project must contain exactly one writable cassette, found: {writableCount}.");
        }

        foreach ((string roleName, RoleDefinition role) in project.Roles)
        {
            ProjectConfigurationValidation.RequireText(roleName, "Role name is required.");
            ArgumentNullException.ThrowIfNull(role);
        }

        foreach (MemberDefinition member in project.Members)
        {
            ProjectConfigurationValidation.RequireText(
                member.UserId,
                "Member userId is required.");
        }
    }

    private static void ValidateCassette(CassetteDefinition cassette)
    {
        ProjectConfigurationValidation.RequireText(cassette.Id, "Cassette id is required.");
        ProjectConfigurationValidation.RequireText(
            cassette.Name,
            $"Cassette name is required for '{cassette.Id}'.");
        ProjectConfigurationValidation.RequireText(
            cassette.Path,
            $"Cassette path is required for '{cassette.Id}'.");

        if (!string.Equals(
                cassette.DefaultAccess,
                ProjectConfigurationRules.NoDefaultAccess,
                StringComparison.Ordinal) &&
            !string.Equals(cassette.DefaultAccess, ProjectRights.Read, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"Unknown defaultAccess '{cassette.DefaultAccess}' for cassette '{cassette.Id}'.");
        }
    }
}
