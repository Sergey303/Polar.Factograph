using Polar.Factograph.Domain;

namespace Polar.Factograph.Application;

public sealed class ProjectConfigurationLoader
{
    public async Task<ProjectDefinition> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        ProjectDefinition project = await ProjectConfigurationJsonReader.ReadAsync(
            fullPath,
            cancellationToken);
        ProjectShapeValidator.Validate(project);
        project = ProjectDefinitionPathResolver.Resolve(
            project,
            Path.GetDirectoryName(fullPath)!);
        IReadOnlySet<string> cassetteIds = ProjectIdentityValidator.Validate(project);
        ProjectRightsValidator.Validate(project, cassetteIds);
        ProjectMemberValidator.Validate(project, cassetteIds);
        ProjectWriteRoutingValidator.Validate(project);
        return project;
    }
}
