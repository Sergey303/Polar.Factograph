using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectRightsValidator
{
    public static void Validate(
        ProjectDefinition project,
        IReadOnlySet<string> cassetteIds)
    {
        foreach ((string roleName, RoleDefinition role) in project.Roles)
        {
            ValidateRights(
                role.ProjectRights,
                ProjectConfigurationRules.KnownProjectRights,
                $"role '{roleName}' project rights");
            ValidateCassetteMap(
                role.CassetteRights,
                cassetteIds,
                $"role '{roleName}' cassette rights");
        }
    }

    internal static void ValidateCassetteMap(
        IReadOnlyDictionary<string, string[]> configured,
        IReadOnlySet<string> cassetteIds,
        string location)
    {
        foreach ((string cassetteId, string[] rights) in configured)
        {
            if (!string.Equals(
                    cassetteId,
                    ProjectConfigurationRules.Wildcard,
                    StringComparison.Ordinal) &&
                !cassetteIds.Contains(cassetteId))
            {
                throw new InvalidDataException(
                    $"Unknown cassette '{cassetteId}' in {location}.");
            }

            ValidateRights(
                rights,
                ProjectConfigurationRules.KnownCassetteRights,
                $"{location} for '{cassetteId}'");
        }
    }

    private static void ValidateRights(
        IReadOnlyList<string> rights,
        IReadOnlySet<string> knownRights,
        string location)
    {
        ProjectConfigurationValidation.RejectDuplicates(rights, location);
        foreach (string right in rights)
        {
            if (!knownRights.Contains(right))
            {
                throw new InvalidDataException($"Unknown right '{right}' in {location}.");
            }
        }
    }
}
