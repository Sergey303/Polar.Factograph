using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Authentication;

public sealed class LocalEditorProvisioningHostedService(
    LocalAuthenticationService authentication,
    LocalAuthenticationOptions options,
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    IdentityProjectMemberOverlay memberOverlay,
    ProjectAccessService accessService,
    ILogger<LocalEditorProvisioningHostedService> logger) : IHostedService
{
    private static readonly IReadOnlySet<string> AllowedPublicProjectRights =
        new HashSet<string>(
            [ProjectRights.Read, ProjectRights.Search],
            StringComparer.Ordinal);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (options.PublicReadEnabled)
            {
                string projectPath = projectPathResolver.GetRequiredPath();
                ProjectDefinition project = await projectLoader.LoadAsync(
                    projectPath,
                    cancellationToken);
                ValidatePublicAccess(project);
            }

            await authentication.ProvisionConfiguredEditorsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Failed to validate public access or reconcile configured editors and their Fog files.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void ValidatePublicAccess(ProjectDefinition project)
    {
        if (!project.Roles.ContainsKey(LocalAuthenticationOptions.PublicViewerRole))
        {
            throw new InvalidOperationException(
                $"Public reading requires project role '{LocalAuthenticationOptions.PublicViewerRole}'.");
        }

        ProjectDefinition overlaid = memberOverlay.Apply(project, options.PublicUserId);
        ProjectAccessSnapshot access = accessService.Evaluate(overlaid, options.PublicUserId);
        if (!access.HasProjectRight(ProjectRights.Read) ||
            !access.HasProjectRight(ProjectRights.Search))
        {
            throw new InvalidOperationException(
                "The public viewer role must grant project rights 'read' and 'search'.");
        }

        string[] unexpectedProjectRights = access.ProjectRights
            .Where(right => !AllowedPublicProjectRights.Contains(right))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unexpectedProjectRights.Length > 0)
        {
            throw new InvalidOperationException(
                $"The public viewer has unsafe project rights: {string.Join(", ", unexpectedProjectRights)}.");
        }

        if (access.DefaultWriteCassetteId is not null)
        {
            throw new InvalidOperationException(
                "The public viewer must not have a default writable cassette.");
        }

        string[] unsafeCassetteRights = access.Cassettes.Values
            .SelectMany(cassette => cassette.Rights
                .Where(right => !string.Equals(right, CassetteRights.Read, StringComparison.Ordinal))
                .Select(right => $"{cassette.CassetteId}:{right}"))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (unsafeCassetteRights.Length > 0)
        {
            throw new InvalidOperationException(
                $"The public viewer has unsafe cassette rights: {string.Join(", ", unsafeCassetteRights)}.");
        }
    }
}
