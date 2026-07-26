using Polar.Factograph.Api.Collections;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class ProjectCollectionAddCoordinator(
    ProjectResourceWriteCoordinator resourceWriter)
{
    public async Task<CollectionItemMutationResponse> AddAsync(
        ProjectAccessContext context,
        CollectionItemAddRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        FogResourceWriteRequest write = new(
            CollectionMutationVocabulary.MembershipType,
            [
                new FogProperty(
                    CollectionMutationVocabulary.InCollection,
                    FogPropertyKind.Resource,
                    request.CollectionId),
                new FogProperty(
                    CollectionMutationVocabulary.CollectionItem,
                    FogPropertyKind.Resource,
                    request.ResourceId)
            ]);
        ProjectResourceWriteOutcome outcome = await resourceWriter.WriteAsync(
            context,
            write,
            request.CassetteId,
            cancellationToken);

        return new CollectionItemMutationResponse(
            outcome.ResourceId,
            request.CollectionId,
            request.ResourceId,
            outcome.CassetteId,
            outcome.ModifiedAtUtc,
            outcome.IndexReady,
            outcome.GenerationId);
    }
}
