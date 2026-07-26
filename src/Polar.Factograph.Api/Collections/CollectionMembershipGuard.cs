using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Collections;

public sealed class CollectionMembershipGuard(ProjectStoreProvider storeProvider)
{
    public async Task RequireMatchAsync(
        ProjectAccessContext context,
        CollectionItemRemoveRequest request,
        CancellationToken cancellationToken)
    {
        PolarDbTypedProjectStore store = storeProvider.GetCurrent(
            context.Project.Index.Path);
        IReadOnlySet<string> readable = context.Access.ReadableCassetteIds;
        ResourceHead? head = await store.GetResourceHeadAsync(
            request.MembershipResourceId,
            cancellationToken);
        if (head is null || head.IsDeleted || !readable.Contains(head.SourceCassetteId))
        {
            throw InvalidMembership();
        }

        IReadOnlyList<string> types = await new ProjectResourceTypeReader(store)
            .ReadAllAsync(request.MembershipResourceId, readable, cancellationToken);
        bool hasType = types.Contains(
            CollectionMutationVocabulary.FullMembershipType,
            StringComparer.Ordinal);
        bool hasCollection = await CollectionMembershipLinkReader.HasAsync(
            store,
            request.MembershipResourceId,
            CollectionMutationVocabulary.FullInCollection,
            request.CollectionId,
            readable,
            cancellationToken);
        bool hasItem = await CollectionMembershipLinkReader.HasAsync(
            store,
            request.MembershipResourceId,
            CollectionMutationVocabulary.FullCollectionItem,
            request.ResourceId,
            readable,
            cancellationToken);
        if (!hasType || !hasCollection || !hasItem)
        {
            throw InvalidMembership();
        }
    }

    private static ArgumentException InvalidMembership() => new(
        "Collection membership does not match the requested collection and item.");
}
