using Polar.Factograph.Api.Collections;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectCollectionRemoveCoordinator(
    IFogDirectiveWriter directiveWriter,
    ProjectCassetteCommandResolver cassetteResolver,
    CollectionMembershipGuard membershipGuard,
    ProjectFogMutationRunner mutationRunner)
{
    public async Task<CollectionItemMutationResponse> RemoveAsync(
        ProjectAccessContext context,
        CollectionItemRemoveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        string cassetteId = cassetteResolver.Resolve(
            context.Access,
            CassetteRights.Delete,
            request.CassetteId);
        FogDirectiveWriteRequest delete = new(
            FogRecordKind.Delete,
            request.MembershipResourceId);
        ProjectFogMutationOutcome<FogDirectiveWriteResult> mutation =
            await mutationRunner.RunAsync(
                context.Project,
                context.Access.UserId,
                cassetteId,
                (source, token) => directiveWriter.AppendAsync(source, delete, token),
                cancellationToken,
                token => membershipGuard.RequireMatchAsync(context, request, token));

        return new CollectionItemMutationResponse(
            request.MembershipResourceId,
            request.CollectionId,
            request.ResourceId,
            cassetteId,
            mutation.Written.ModifiedAtUtc,
            mutation.IndexReady,
            mutation.GenerationId);
    }
}
