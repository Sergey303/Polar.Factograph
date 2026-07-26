using Microsoft.Extensions.Logging.Abstractions;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api.Tests;

internal sealed class WritableApiMutationHarness
{
    public WritableApiMutationHarness()
    {
        FileSystemFogSourceScanner scanner = new();
        ProjectOperationGate gate = new();
        DirtyMarker = new ProjectIndexDirtyMarker();
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
        ProjectResourceWriteValidationService validation = new(
            new OntologyCatalogProvider(new XmlOntologyCatalogLoader()),
            new OntologyResourceWriteValidator());

        Resources = new ProjectResourceWriteCoordinator(
            new FileSystemFogResourceWriter(),
            new ProjectWriteCassetteResolver(),
            validation,
            runner);
        Directives = new ProjectDirectiveWriteCoordinator(
            new FileSystemFogDirectiveWriter(),
            new ProjectCassetteCommandResolver(),
            runner);
    }

    public ProjectIndexDirtyMarker DirtyMarker { get; }
    public ProjectResourceWriteCoordinator Resources { get; }
    public ProjectDirectiveWriteCoordinator Directives { get; }
}
