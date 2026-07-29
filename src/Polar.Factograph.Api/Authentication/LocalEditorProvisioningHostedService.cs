namespace Polar.Factograph.Api.Authentication;

public sealed class LocalEditorProvisioningHostedService(
    LocalAuthenticationService authentication,
    ILogger<LocalEditorProvisioningHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await authentication.ProvisionConfiguredEditorsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Failed to reconcile configured editors and their Fog files.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
