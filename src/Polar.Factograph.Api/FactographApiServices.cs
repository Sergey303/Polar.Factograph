using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
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
        services.AddSingleton<IFogSourceScanner, FileSystemFogSourceScanner>();
        services.AddSingleton<IFogRecordReader, FileSystemFogRecordReader>();
        services.AddSingleton<FogProjectRecordSource>();
        services.AddSingleton<LegacyFogProjectMaterializer>();
        services.AddSingleton<CassetteDocumentPathResolver>();
        services.AddSingleton<ProjectIndexRebuilder>();

        services.AddSingleton<ProjectPathResolver>();
        services.AddSingleton<CurrentUserResolver>();
        services.AddSingleton<ProjectStoreProvider>();
        services.AddSingleton<ProjectRequestContextFactory>();
        services.AddSingleton<ProjectIndexCoordinator>();
        services.AddSingleton<DocumentContentTypeResolver>();
        return services;
    }
}
