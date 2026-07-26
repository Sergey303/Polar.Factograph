using Polar.Factograph.Storage;

namespace Polar.Factograph.Application;

public sealed class ProjectResourceTypeReader(IProjectRdfStore rdfStore)
{
    private const string RdfType = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

    public async Task<string?> ReadAsync(
        string resourceId,
        IReadOnlySet<string> cassetteIds,
        CancellationToken cancellationToken)
    {
        List<string> types = new();
        await foreach (TripleRow triple in rdfStore.FindAsync(
                           new TriplePattern(
                               Subject: resourceId,
                               Predicate: RdfType,
                               ObjectKind: TripleObjectKind.Iri),
                           cassetteIds,
                           cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            types.Add(triple.ObjectValue);
        }

        return types.Order(StringComparer.Ordinal).FirstOrDefault();
    }
}
