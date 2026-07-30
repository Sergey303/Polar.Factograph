using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Api.Writes;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Endpoints;

public static class OntologyEndpoints
{
    public static IEndpointRouteBuilder MapOntologyEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/ontology/write-schema", GetWriteSchemaAsync);
        return endpoints;
    }

    private static async Task<IResult> GetWriteSchemaAsync(
        string? lang,
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        OntologyCatalogProvider catalogs,
        OntologyWriteSchemaBuilder builder,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        _ = ProjectAuthorization.RequireWritableCassetteRight(
            context.Access,
            CassetteRights.WriteMetadata);
        OntologyCatalog catalog = await catalogs.GetAsync(
            context.Project.Ontology.Path,
            cancellationToken);
        string language = string.IsNullOrWhiteSpace(lang) ? "ru" : lang;
        return Results.Ok(builder.Build(catalog, language));
    }
}
