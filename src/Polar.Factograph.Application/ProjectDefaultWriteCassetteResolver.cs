using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

internal static class ProjectDefaultWriteCassetteResolver
{
    public static string? Resolve(
        ProjectDefinition project,
        IReadOnlyDictionary<string, CassetteAccessSnapshot> cassetteAccess)
    {
        CassetteDefinition[] writable = project.Cassettes
            .Where(cassette => cassette.Enabled && cassette.AllowWrite)
            .ToArray();

        if (writable.Length != 1)
        {
            throw new InvalidDataException(
                $"Project must contain exactly one writable cassette, found: {writable.Length}.");
        }

        CassetteDefinition cassette = writable[0];
        return cassetteAccess.TryGetValue(cassette.Id, out CassetteAccessSnapshot? access) &&
               access.Enabled &&
               access.AllowWrite &&
               ProjectWriteRights.HasAny(access.Rights)
            ? cassette.Id
            : null;
    }
}
