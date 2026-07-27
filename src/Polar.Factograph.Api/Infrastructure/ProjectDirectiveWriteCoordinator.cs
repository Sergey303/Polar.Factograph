using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectDirectiveWriteCoordinator(
    IFogDirectiveWriter directiveWriter,
    ProjectCassetteCommandResolver cassetteResolver,
    ProjectFogMutationRunner mutationRunner)
{
    public Task<ProjectDirectiveWriteOutcome> DeleteAsync(
        ProjectAccessContext context,
        FogDirectiveWriteRequest request,
        string? requestedCassetteId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            context,
            request,
            CassetteRights.Delete,
            requestedCassetteId,
            cancellationToken);

    public Task<ProjectDirectiveWriteOutcome> SubstituteAsync(
        ProjectAccessContext context,
        FogDirectiveWriteRequest request,
        string? requestedCassetteId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            context,
            request,
            CassetteRights.Substitute,
            requestedCassetteId,
            cancellationToken);

    private async Task<ProjectDirectiveWriteOutcome> ExecuteAsync(
        ProjectAccessContext context,
        FogDirectiveWriteRequest request,
        string requiredRight,
        string? requestedCassetteId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            requiredRight,
            requestedCassetteId);
        ProjectFogMutationOutcome<FogDirectiveWriteResult> mutation =
            await mutationRunner.RunAsync(
                context.Project,
                context.Access.UserId,
                cassetteId,
                (source, token) => directiveWriter.AppendAsync(source, request, token),
                cancellationToken);

        return new ProjectDirectiveWriteOutcome(
            request.Kind.ToString().ToLowerInvariant(),
            mutation.Written.ResourceId,
            mutation.Written.SubstituteTargetId,
            cassetteId,
            mutation.Written.ModifiedAtUtc,
            mutation.IndexReady,
            mutation.GenerationId);
    }
}
