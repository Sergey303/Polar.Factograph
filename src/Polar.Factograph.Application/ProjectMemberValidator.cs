using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectMemberValidator
{
    public static void Validate(
        ProjectDefinition project,
        IReadOnlySet<string> cassetteIds)
    {
        foreach (MemberDefinition member in project.Members)
        {
            ProjectConfigurationValidation.RejectDuplicates(
                member.Roles,
                $"roles for user '{member.UserId}'");
            foreach (string roleName in member.Roles)
            {
                if (!project.Roles.ContainsKey(roleName))
                {
                    throw new InvalidDataException(
                        $"Unknown role '{roleName}' for user '{member.UserId}'.");
                }
            }

            ProjectRightsValidator.ValidateCassetteMap(
                member.CassetteOverrides,
                cassetteIds,
                $"cassette overrides for user '{member.UserId}'");
        }
    }
}
