namespace Polar.Factograph.Application;

public sealed class CassetteAuthorizationException : UnauthorizedAccessException
{
    public CassetteAuthorizationException(
        string userId,
        string cassetteId,
        string requiredRight)
        : base(
            $"User '{userId}' does not have required cassette right '{requiredRight}' for '{cassetteId}'.")
    {
        UserId = userId;
        CassetteId = cassetteId;
        RequiredRight = requiredRight;
    }

    public string UserId { get; }
    public string CassetteId { get; }
    public string RequiredRight { get; }
}
