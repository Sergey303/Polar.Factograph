using Polar.Factograph.Api.Endpoints;
using Polar.Factograph.Api.Infrastructure;

namespace Polar.Factograph.Api;

public static class FactographEndpointMapping
{
    private static readonly string[] ApiMethods =
    [
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
        HttpMethods.Options,
        HttpMethods.Head
    ];

    public static WebApplication MapFactographApi(this WebApplication app)
    {
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseMiddleware<DynamicBaseUrlMiddleware>();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapLegacyPageEndpoints();
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
        app.MapMethods("/api/{**path}", ApiMethods, () => Results.NotFound());
        app.MapFallbackToFile("index.html");
        return app;
    }
}
