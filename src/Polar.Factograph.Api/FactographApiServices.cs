using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api;

public static class FactographApiServices
{
    public static IServiceCollection AddFactographApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddFactographAuthentication(configuration);
        services.AddSingleton<ProjectConfigurationLoader>();
        services.AddSingleton<ProjectAccessService>();
        services.AddSingleton<ProjectWriteCassetteResolver>();
        services.AddSingleton<ProjectCassetteCommandResolver>();
        services.AddSingleton<XmlOntologyCatalogLoader>();
        services.AddSingleton<OntologyResourceWriteValidator>();
        services.AddSingleton<IFogSourceScanner, FileSystemFogSourceScanner>();
        services.AddSingleton<IFogRecordReader, FileSystemFogRecordReader>();
        services.AddSingleton<IFogResourceWriter, FileSystemFogResourceWriter>();
        services.AddSingleton<IFogDirectiveWriter, FileSystemFogDirectiveWriter>();
        services.AddSingleton<FogProjectRecordSource>();
        services.AddSingleton<LegacyFogProjectMaterializer>();
        services.AddSingleton<CassetteDocumentPathResolver>();
        services.AddSingleton<ProjectIndexRebuilder>();

        services.AddSingleton<ProjectPathResolver>();
        services.AddSingleton<CurrentUserResolver>();
        services.AddSingleton<ProjectOperationGate>();
        services.AddSingleton<ProjectIndexDirtyMarker>();
        services.AddSingleton<ProjectStoreProvider>();
        services.AddSingleton<OntologyCatalogProvider>();
        services.AddSingleton<ProjectRequestContextFactory>();
        services.AddSingleton<ProjectIndexCoordinator>();
        services.AddSingleton<ProjectWriteIndexRefresher>();
        services.AddSingleton<ProjectFogMutationRunner>();
        services.AddSingleton<ProjectResourceWriteValidationService>();
        services.AddSingleton<ProjectResourceWriteCoordinator>();
        services.AddSingleton<ProjectDirectiveWriteCoordinator>();
        services.AddSingleton<DocumentContentTypeResolver>();
        return services;
    }
}
