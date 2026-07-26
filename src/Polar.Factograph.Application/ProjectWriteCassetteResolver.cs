using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectWriteCassetteResolver
{
    public string Resolve(
        ProjectAccessSnapshot access,
        string? requestedCassetteId = null)
    {
        ArgumentNullException.ThrowIfNull(access);

        string cassetteId = string.IsNullOrWhiteSpace(requestedCassetteId)
            ? access.DefaultWriteCassetteId
                ?? throw new InvalidOperationException(
                    "No default writable cassette is available for the current user.")
            : requestedCassetteId;

        if (!access.HasCassetteRight(cassetteId, CassetteRights.WriteMetadata))
        {
            throw new CassetteAuthorizationException(
                access.UserId,
                cassetteId,
                CassetteRights.WriteMetadata);
        }

        return cassetteId;
    }
}
