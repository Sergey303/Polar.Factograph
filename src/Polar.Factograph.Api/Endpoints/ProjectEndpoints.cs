using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Endpoints;

public sealed record ProjectCassetteOverview(
    string Id,
    string Name,
    bool AllowWrite,
    IReadOnlyList<string> Rights);

public sealed record ProjectOverview(
    string ProjectId,
    string Name,
    string UserId,
    IReadOnlyList<string> ProjectRights,
    IReadOnlyList<ProjectCassetteOverview> Cassettes,
    string? DefaultWriteCassetteId);

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/project", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        ProjectRequestContextFactory contextFactory,
        CancellationToken cancellationToken)
    {
        ProjectAccessContext context = await contextFactory.CreateAccessAsync(
            httpContext,
            cancellationToken);
        _ = ProjectAuthorization.RequireRead(context.Access);

        ProjectCassetteOverview[] cassettes = context.Project.Cassettes
            .Where(cassette => context.Access.ReadableCassetteIds.Contains(cassette.Id))
            .OrderBy(cassette => cassette.Name, StringComparer.OrdinalIgnoreCase)
            .Select(cassette => new ProjectCassetteOverview(
                cassette.Id,
                cassette.Name,
                cassette.AllowWrite,
                context.Access.Cassettes[cassette.Id].Rights
                    .Order(StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        return Results.Ok(new ProjectOverview(
            context.Project.ProjectId,
            context.Project.Name,
            context.Access.UserId,
            context.Access.ProjectRights.Order(StringComparer.Ordinal).ToArray(),
            cassettes,
            context.Access.DefaultWriteCassetteId));
    }
}
