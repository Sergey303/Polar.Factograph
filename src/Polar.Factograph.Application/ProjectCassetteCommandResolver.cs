using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectCassetteCommandResolver
{
    public string Resolve(
        ProjectAccessSnapshot access,
        string requiredRight,
        string? requestedCassetteId = null)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(requiredRight);
        string cassetteId = string.IsNullOrWhiteSpace(requestedCassetteId)
            ? access.DefaultWriteCassetteId
                ?? throw new InvalidOperationException(
                    "No default writable cassette is available for the current user.")
            : requestedCassetteId;

        if (!access.HasCassetteRight(cassetteId, requiredRight))
        {
            throw new UnauthorizedAccessException(
                $"Cassette right '{requiredRight}' is not granted for '{cassetteId}'.");
        }

        return cassetteId;
    }
}
