using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Endpoints;

public static class ProjectDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapProjectDiagnosticsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/admin/project");
        group.MapGet("/sources", GetSourcesAsync);
        group.MapGet("/materialization-summary", GetSummaryAsync);
        return endpoints;
    }

    private static async Task<IResult> GetSourcesAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        IFogSourceScanner scanner,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectAuthorization.RequireProjectRight(
            context.Access,
            ProjectRights.RebuildIndex);

        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(
            context.Project,
            cancellationToken);
        return Results.Ok(sources);
    }

    private static async Task<IResult> GetSummaryAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        IFogSourceScanner scanner,
        FogProjectRecordSource recordSource,
        LegacyFogProjectMaterializer materializer,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        ProjectAuthorization.RequireProjectRight(
            context.Access,
            ProjectRights.RebuildIndex);

        IReadOnlyList<FogSourceDescriptor> sources = await scanner.ScanAsync(
            context.Project,
            cancellationToken);
        FogRecordStreamFactory openRecords = token => recordSource.ReadAsync(sources, token);
        FogMaterializationStatistics summary = await materializer.SummarizeAsync(
            sources.Count,
            openRecords,
            cancellationToken);
        return Results.Ok(summary);
    }
}
