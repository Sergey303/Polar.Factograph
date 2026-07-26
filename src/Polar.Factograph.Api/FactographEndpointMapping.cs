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
        app.MapProjectEndpoints();
        app.MapProjectDiagnosticsEndpoints();
        app.MapIndexEndpoints();
        app.MapResourceEndpoints();
        app.MapResourceWriteEndpoints();
        app.MapSearchEndpoints();
        app.MapCollectionEndpoints();
        app.MapDocumentEndpoints();
        return app;
    }
}
