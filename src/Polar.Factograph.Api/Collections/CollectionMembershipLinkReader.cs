using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Collections;

internal static class CollectionMembershipLinkReader
{
    public static async Task<bool> HasAsync(
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
}
