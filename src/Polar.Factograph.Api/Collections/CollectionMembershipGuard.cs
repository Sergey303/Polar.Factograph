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
        bool hasType = types.Any(type => string.Equals(
            type,
            CollectionMutationVocabulary.FullMembershipType,
            StringComparison.Ordinal));
        bool hasCollection = await HasLinkAsync(
            store,
            request.MembershipResourceId,
            CollectionMutationVocabulary.FullInCollection,
            request.CollectionId,
            readable,
            cancellationToken);
        bool hasItem = await HasLinkAsync(
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

    private static async Task<bool> HasLinkAsync(
        IProjectRdfStore store,
        string membershipId,
        string predicate,
        string targetId,
        IReadOnlySet<string> readableCassetteIds,
        CancellationToken cancellationToken)
    {
        await foreach (TripleRow _ in store.FindAsync(
                           new TriplePattern(
                               Subject: membershipId,
                               Predicate: predicate,
                               ObjectKind: TripleObjectKind.Iri,
                               ObjectValue: targetId),
                           readableCassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            return true;
        }

        return false;
    }

    private static ArgumentException InvalidMembership() => new(
        "Collection membership does not match the requested collection and item.");
}
