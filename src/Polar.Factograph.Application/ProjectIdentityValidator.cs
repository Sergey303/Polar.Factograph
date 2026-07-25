using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectIdentityValidator
{
    public static IReadOnlySet<string> Validate(ProjectDefinition project)
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

        foreach (MemberDefinition member in project.Members)
        {
            ValidateMemberRoles(project, member);
        }

        return cassetteIds;
    }

    private static void ValidateMemberRoles(
        ProjectDefinition project,
        MemberDefinition member)
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
    }
}
