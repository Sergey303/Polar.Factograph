using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Writing;

public sealed class ProjectResourceWriteCoordinator(
    IFogSourceScanner sourceScanner,
    ProjectWriteCassetteResolver cassetteResolver,
    IFogResourceWriter writer,
    IProjectIndexRefresher indexRefresher,
    ProjectMutationGate mutationGate)
{
    public async Task<ProjectResourceWriteResult> WriteAsync(
        ProjectDefinition project,
        ProjectAccessSnapshot access,
        ProjectResourceWriteCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(command);

        await using ProjectMutationLease lease = await mutationGate.AcquireAsync(
            project.Index.Path,
            cancellationToken);
        string cassetteId = cassetteResolver.Resolve(access, command.CassetteId);
        IReadOnlyList<FogSourceDescriptor> sources = await sourceScanner.ScanAsync(
            project,
            cancellationToken);
        FogSourceDescriptor source = FogWritableSourceSelector.Select(sources, cassetteId);
        FogResourceWriteResult written = await writer.AppendAsync(
            source,
            command.Resource,
            cancellationToken);

        ProjectIndexRebuildResult rebuilt;
        try
        {
            rebuilt = await indexRefresher.RebuildAsync(project, cancellationToken);
        }
        catch (Exception exception)
        {
            throw new ProjectWriteCommittedException(written.ResourceId, exception);
        }

        return new ProjectResourceWriteResult(
            written.ResourceId,
            cassetteId,
            written.ModifiedAtUtc,
            rebuilt.GenerationId,
            rebuilt.SourceFiles,
            rebuilt.Statistics);
    }
}
