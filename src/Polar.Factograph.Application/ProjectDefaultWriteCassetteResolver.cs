using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectDefaultWriteCassetteResolver
{
    public static string? Resolve(
        ProjectDefinition project,
        MemberDefinition member,
        IReadOnlyDictionary<string, CassetteAccessSnapshot> cassetteAccess)
    {
        foreach (string roleName in member.Roles)
        {
            if (!project.WriteRouting.DefaultCassetteByRole.TryGetValue(
                    roleName,
                    out string? cassetteId) ||
                !cassetteAccess.TryGetValue(cassetteId, out CassetteAccessSnapshot? access) ||
                !access.Enabled ||
                !access.AllowWrite ||
                !ProjectWriteRights.HasAny(access.Rights))
            {
                continue;
            }

            return cassetteId;
        }

        return null;
    }
}
