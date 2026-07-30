using Polar.Factograph.Application;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Infrastructure;

public sealed class OntologyClassSearchServiceProvider
{
    private readonly object _sync = new();
    private PolarDbTypedProjectStore? _store;
    private OntologyCatalog? _ontology;
    private OntologyClassSearchService? _service;

    public OntologyClassSearchService Get(
        PolarDbTypedProjectStore store,
        OntologyCatalog ontology)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(ontology);

        lock (_sync)
        {
            if (ReferenceEquals(_store, store) &&
                ReferenceEquals(_ontology, ontology) &&
                _service is not null)
            {
                return _service;
            }

            _store = store;
            _ontology = ontology;
            _service = new OntologyClassSearchService(store, store, ontology);
            return _service;
        }
    }
}
