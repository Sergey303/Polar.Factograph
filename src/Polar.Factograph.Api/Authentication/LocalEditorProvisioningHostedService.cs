using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Api.Authentication;

public sealed class LocalEditorProvisioningHostedService(
    LocalAuthenticationService authentication,
    LocalAuthenticationOptions options,
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    ILogger<LocalEditorProvisioningHostedService> logger) : IHostedService
{
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
                if (!project.Roles.ContainsKey(LocalAuthenticationOptions.PublicViewerRole))
                {
                    throw new InvalidOperationException(
                        $"Public reading requires project role '{LocalAuthenticationOptions.PublicViewerRole}'.");
                }
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
}
