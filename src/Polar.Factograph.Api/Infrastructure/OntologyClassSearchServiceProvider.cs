using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Polar.Factograph.Application;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class OntologyClassSearchServiceProvider
{
    private readonly ConditionalWeakTable<
        PolarDbTypedProjectStore,
        ConcurrentDictionary<OntologyCatalog, OntologyClassSearchService>> _services = new();

    public OntologyClassSearchService Get(
        PolarDbTypedProjectStore store,
        OntologyCatalog ontology)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(ontology);

        ConcurrentDictionary<OntologyCatalog, OntologyClassSearchService> byOntology =
            _services.GetValue(
                store,
                _ => new ConcurrentDictionary<OntologyCatalog, OntologyClassSearchService>());
        return byOntology.GetOrAdd(
            ontology,
            value => new OntologyClassSearchService(store, store, value));
    }
}
