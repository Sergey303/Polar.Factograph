using Polar.Factograph.Api.Authentication;
using Polar.Factograph.Api.Collections;
using Polar.Factograph.Api.Documents;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Previews;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Fog;
using Polar.Factograph.Storage;

namespace Polar.Factograph.Api;

public static class FactographApiServices
{
    public static IServiceCollection AddFactographApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        LocalAuthenticationOptions authentication = services.AddLocalIdentity(
            configuration,
            environment);
        services.AddFactographAuthentication(authentication, environment);
        services.AddOptions<DocumentUploadOptions>()
            .Bind(configuration.GetSection(DocumentUploadOptions.SectionName))
            .Validate(
                options => options.MaxUploadBytes > 0,
                "Documents:MaxUploadBytes must be positive.")
            .ValidateOnStart();
        services.AddOptions<PreviewWorkerOptions>()
            .Bind(configuration.GetSection(PreviewWorkerOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "Previews configuration is invalid.")
            .ValidateOnStart();
        services.AddSingleton<IdentityJsonStore>();
        services.AddSingleton<LocalAuthenticationService>();
        services.AddSingleton<IdentityProjectMemberOverlay>();
        services.AddSingleton<IdentityFogSourceResolver>();
        services.AddSingleton<ProjectConfigurationLoader>();
        services.AddSingleton<ProjectAccessService>();
        services.AddSingleton<ProjectWriteCassetteResolver>();
        services.AddSingleton<ProjectCassetteCommandResolver>();
        services.AddSingleton<XmlOntologyCatalogLoader>();
        services.AddSingleton<OntologyWriteSchemaBuilder>();
        services.AddSingleton<OntologyResourceWriteValidator>();
        services.AddSingleton<OntologyObjectTargetValidator>();
        services.AddSingleton<OntologyValidationService>();
        services.AddSingleton<IFogSourceScanner, FileSystemFogSourceScanner>();
        services.AddSingleton<IFogRecordReader, FileSystemFogRecordReader>();
        services.AddSingleton<IFogResourceWriter, FileSystemFogResourceWriter>();
        services.AddSingleton<IFogDirectiveWriter, FileSystemFogDirectiveWriter>();
        services.AddSingleton<ICassetteDocumentWriter, FileSystemCassetteDocumentWriter>();
        services.AddSingleton<ICassetteNamedFogWriter, FileSystemCassetteNamedFogWriter>();
        services.AddSingleton<ICassettePreviewRequestWriter, FileSystemCassettePreviewRequestWriter>();
        services.AddSingleton<FileSystemCassettePreviewQueueProcessor>();
        services.AddSingleton<CassettePreviewQueueStatusReader>();
        services.AddSingleton<FogProjectRecordSource>();
        services.AddSingleton<LegacyFogProjectMaterializer>();
        services.AddSingleton<CassetteDocumentPathResolver>();
        services.AddSingleton<ProjectIndexRebuilder>();

        services.AddSingleton<ProjectPathResolver>();
        services.AddSingleton<CurrentUserResolver>();
        services.AddSingleton<ProjectOperationGate>();
        services.AddSingleton<ProjectIndexDirtyMarker>();
        services.AddSingleton<ProjectIndexRuntimeStatusReader>();
        services.AddSingleton<ProjectStoreProvider>();
        services.AddSingleton<OntologyCatalogProvider>();
        services.AddSingleton<OntologyClassSearchServiceProvider>();
        services.AddSingleton<ProjectRequestContextFactory>();
        services.AddSingleton<ResourceHtmlMetadataProvider>();
        services.AddSingleton<ProjectIndexCoordinator>();
        services.AddSingleton<ProjectFullRefreshCoordinator>();
        services.AddSingleton<ProjectIndexVerificationCoordinator>();
        services.AddSingleton<ProjectWriteIndexRefresher>();
        services.AddSingleton<ProjectFogMutationRunner>();
        services.AddSingleton<ProjectResourceWriteValidationService>();
        services.AddSingleton<ProjectResourceTargetValidationService>();
        services.AddSingleton<ProjectResourceWriteCoordinator>();
        services.AddSingleton<ProjectDirectiveWriteCoordinator>();
        services.AddSingleton<CollectionMembershipGuard>();
        services.AddSingleton<ProjectCollectionAddCoordinator>();
        services.AddSingleton<ProjectCollectionRemoveCoordinator>();
        services.AddSingleton<ProjectDocumentAddCoordinator>();
        services.AddSingleton<ProjectDocumentReplaceCoordinator>();
        services.AddSingleton<DocumentContentTypeResolver>();
        services.AddSingleton<PreviewWorkerRuntimeState>();
        services.AddSingleton<ICassettePreviewRenderer, ExternalProcessPreviewRenderer>();
        services.AddSingleton<PreviewWorkerCycle>();
        services.AddHostedService<LocalEditorProvisioningHostedService>();
        services.AddHostedService<ProjectIndexInitializationHostedService>();
        services.AddHostedService<PreviewQueueHostedService>();
        return services;
    }
}
