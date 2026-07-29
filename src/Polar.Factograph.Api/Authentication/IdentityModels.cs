namespace Polar.Factograph.Api.Authentication;

public sealed record IdentityData
{
    public int SchemaVersion { get; init; } = 1;
    public IdentityUser[] Users { get; init; } = Array.Empty<IdentityUser>();
    public IdentityDevice[] Devices { get; init; } = Array.Empty<IdentityDevice>();
}

public sealed record IdentityUser
{
    public required string Id { get; init; }
    public required string Login { get; init; }
    public required string NormalizedLogin { get; init; }
    public required string DisplayName { get; init; }
    public required string PasswordHash { get; init; }
    public bool Enabled { get; init; } = true;
    public int SecurityVersion { get; init; } = 1;
    public string[] Roles { get; init; } = Array.Empty<string>();
    public IdentityFogReference? Fog { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}

public sealed record IdentityFogReference
{
    public required string CassetteId { get; init; }
    public required string DocumentUri { get; init; }
    public required string RelativePath { get; init; }
}

public sealed record IdentityDevice
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset LastSeenAtUtc { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset? RevokedAtUtc { get; init; }
}

public sealed record LocalAuthenticationSession(
    IdentityUser User,
    IdentityDevice Device);
