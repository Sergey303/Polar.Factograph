using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Infrastructure;

internal static class ProjectCassetteDefinitionResolver
{
    public static CassetteDefinition Require(
        ProjectDefinition project,
        string cassetteId)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(cassetteId);
        return project.Cassettes.FirstOrDefault(cassette =>
                   cassette.Enabled &&
                   string.Equals(cassette.Id, cassetteId, StringComparison.Ordinal))
               ?? throw new KeyNotFoundException(
                   $"Enabled cassette was not found in project '{project.ProjectId}': {cassetteId}");
    }
}
