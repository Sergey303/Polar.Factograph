using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal sealed class WritableApiMutationHarness : IDisposable
{
    private readonly ProjectStoreProvider _stores;

    public WritableApiMutationHarness()
    {
        FileSystemFogSourceScanner scanner = new();
        ProjectOperationGate gate = new();
        DirtyMarker = new ProjectIndexDirtyMarker();
        _stores = new ProjectStoreProvider(DirtyMarker);
        ProjectIndexCoordinator indexCoordinator = new(
            scanner,
            new FogProjectRecordSource(new FileSystemFogRecordReader()),
            new LegacyFogProjectMaterializer(),
            new ProjectIndexRebuilder(),
            gate,
            DirtyMarker);
        ProjectWriteIndexRefresher refresher = new(
            indexCoordinator,
            DirtyMarker,
            NullLogger<ProjectWriteIndexRefresher>.Instance);
        ProjectFogMutationRunner runner = new(
            scanner,
            gate,
            DirtyMarker,
            refresher);
        OntologyCatalogProvider ontology = new(new XmlOntologyCatalogLoader());

        Resources = new ProjectResourceWriteCoordinator(
            new FileSystemFogResourceWriter(),
            new ProjectWriteCassetteResolver(),
            new ProjectResourceWriteValidationService(
                ontology,
                new OntologyResourceWriteValidator()),
            new ProjectResourceTargetValidationService(
                _stores,
                ontology,
                new OntologyObjectTargetValidator()),
            runner);
        Directives = new ProjectDirectiveWriteCoordinator(
            new FileSystemFogDirectiveWriter(),
            new ProjectCassetteCommandResolver(),
            runner);
    }

    public ProjectIndexDirtyMarker DirtyMarker { get; }
    public ProjectResourceWriteCoordinator Resources { get; }
    public ProjectDirectiveWriteCoordinator Directives { get; }

    public void Dispose() => _stores.Dispose();
}
