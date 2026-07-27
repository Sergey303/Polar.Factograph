using Polar.Factograph.Application;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectResourceWriteCoordinator(
    IFogResourceWriter resourceWriter,
    ProjectWriteCassetteResolver cassetteResolver,
    ProjectResourceWriteValidationService validationService,
    ProjectResourceTargetValidationService targetValidationService,
    ProjectFogMutationRunner mutationRunner)
{
    public async Task<ProjectResourceWriteOutcome> WriteAsync(
        ProjectAccessContext context,
        FogResourceWriteRequest request,
        string? requestedCassetteId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            requestedCassetteId);
        await validationService.ValidateAsync(
            context.Project,
            request,
            cancellationToken);
        ProjectFogMutationOutcome<FogResourceWriteResult> mutation =
            await mutationRunner.RunAsync(
                context.Project,
                context.Access.UserId,
                cassetteId,
                (source, token) => resourceWriter.AppendAsync(source, request, token),
                cancellationToken,
                token => targetValidationService.ValidateAsync(context, request, token));

        return new ProjectResourceWriteOutcome(
            mutation.Written.ResourceId,
            cassetteId,
            mutation.Written.ModifiedAtUtc,
            mutation.IndexReady,
            mutation.GenerationId);
    }
}
