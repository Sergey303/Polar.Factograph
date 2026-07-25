using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api;

public static class FactographEndpointMapping
{
    public static WebApplication MapFactographApi(this WebApplication app)
    {
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.MapSystemEndpoints();
        app.MapProjectEndpoints();
        app.MapProjectDiagnosticsEndpoints();
        app.MapIndexEndpoints();
        app.MapResourceEndpoints();
        app.MapSearchEndpoints();
        app.MapDocumentEndpoints();
        return app;
    }
}
