using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Collections;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal sealed class WritableApiMutationHarness : IDisposable
{
    private readonly ProjectStoreProvider _stores;
    private readonly ProjectIndexCoordinator _indexCoordinator;

    public WritableApiMutationHarness()
    {
        FileSystemFogSourceScanner scanner = new();
        ProjectOperationGate gate = new();
        DirtyMarker = new ProjectIndexDirtyMarker();
        _stores = new ProjectStoreProvider(DirtyMarker);
        _indexCoordinator = new ProjectIndexCoordinator(
            scanner,
            new FogProjectRecordSource(new FileSystemFogRecordReader()),
            new LegacyFogProjectMaterializer(),
            new ProjectIndexRebuilder(),
            gate,
            DirtyMarker);
        ProjectWriteIndexRefresher refresher = new(
            _indexCoordinator,
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
                _stores,
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
        CollectionsAdd = new ProjectCollectionAddCoordinator(Resources);
        CollectionsRemove = new ProjectCollectionRemoveCoordinator(
            new FileSystemFogDirectiveWriter(),
            new ProjectCassetteCommandResolver(),
            new CollectionMembershipGuard(_stores),
            runner);
    }

    public ProjectIndexDirtyMarker DirtyMarker { get; }
    public ProjectResourceWriteCoordinator Resources { get; }
    public ProjectDirectiveWriteCoordinator Directives { get; }
    public ProjectCollectionAddCoordinator CollectionsAdd { get; }
    public ProjectCollectionRemoveCoordinator CollectionsRemove { get; }

    public Task RebuildAsync(ProjectDefinition project) =>
        _indexCoordinator.RebuildAsync(project);

    public void Dispose() => _stores.Dispose();
}
