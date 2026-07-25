namespace Polar.Factograph.Application;

public sealed class AuthorizedPresentedPortraitService(
    AuthorizedProjectReadService reads,
    OntologyResourcePortraitPresenter presenter)
{
    public async ValueTask<PresentedProjectResourcePortrait?> GetAsync(
        string resourceId,
        ProjectAccessSnapshot access,
        string preferredLanguage = "ru",
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceId);
        ArgumentNullException.ThrowIfNull(access);
        ArgumentException.ThrowIfNullOrWhiteSpace(preferredLanguage);

        ProjectResourcePortrait? portrait = await reads.GetPortraitAsync(
            resourceId,
            access,
            cancellationToken);

        return portrait is null
            ? null
            : presenter.Present(portrait, preferredLanguage);
    }
}
