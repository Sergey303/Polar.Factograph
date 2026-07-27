using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api;

public static class FactographEndpointMapping
{
    public static WebApplication MapFactographApi(this WebApplication app)
    {
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapSystemEndpoints();
        app.MapAuthenticationEndpoints();
        app.MapProjectEndpoints();
        app.MapProjectDiagnosticsEndpoints();
        app.MapIndexEndpoints();
        app.MapPreviewQueueEndpoints();
        app.MapOntologyEndpoints();
        app.MapResourceEndpoints();
        app.MapResourceWriteEndpoints();
        app.MapResourceDirectiveEndpoints();
        app.MapSearchEndpoints();
        app.MapCollectionEndpoints();
        app.MapCollectionMutationEndpoints();
        app.MapDocumentEndpoints();
        app.MapDocumentWriteEndpoints();
        return app;
    }
}
