using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Authentication;

public sealed class IdentityFogSourceResolver(IdentityJsonStore store)
{
    public FogSourceDescriptor Resolve(
        ProjectDefinition project,
        IReadOnlyList<FogSourceDescriptor> sources,
        string userId,
        string cassetteId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);

        IdentityUser? user = store.FindUser(userId);
        if (user is null)
        {
            return FogWritableSourceSelector.Select(sources, cassetteId);
        }

        if (!string.Equals(user.Fog.CassetteId, cassetteId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"User '{userId}' has no writable Fog in cassette '{cassetteId}'.");
        }

        CassetteDefinition cassette = project.Cassettes.SingleOrDefault(value =>
                string.Equals(value.Id, cassetteId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"Cassette '{cassetteId}' is absent from the project.");
        string expectedPath = Path.GetFullPath(user.Fog.RelativePath, cassette.Path);
        FogSourceDescriptor? source = sources.FirstOrDefault(value =>
            value.Writable &&
            string.Equals(value.CassetteId, cassetteId, StringComparison.Ordinal) &&
            string.Equals(value.Owner, userId, StringComparison.Ordinal) &&
            string.Equals(
                Path.GetFullPath(value.FogPath),
                expectedPath,
                StringComparison.OrdinalIgnoreCase));

        return source ?? throw new InvalidOperationException(
            $"The writable Fog assigned to user '{userId}' was not found or has another owner.");
    }
}
