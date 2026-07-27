namespace Polar.Factograph.Api.Authentication;

public sealed record LocalAuthenticationOptions(
    string IdentityPath,
    string DataProtectionKeysPath,
    string CookieName,
    string DefaultRole,
    string DefaultCassetteId,
    bool RegistrationEnabled,
    int SessionDays,
    long MaxFogBytes)
{
    private const string Section = "Authentication:Local";

    public static LocalAuthenticationOptions Read(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        string identityPath = ResolvePath(
            configuration[$"{Section}:IdentityPath"] ?? "project-data/identity.json",
            environment.ContentRootPath);
        string keyPath = ResolvePath(
            configuration[$"{Section}:DataProtectionKeysPath"] ?? "project-data/data-protection-keys",
            environment.ContentRootPath);
        string defaultRole = configuration[$"{Section}:DefaultRole"]?.Trim() ?? "editor";
        string defaultCassetteId = configuration[$"{Section}:DefaultCassetteId"]?.Trim() ?? string.Empty;
        int sessionDays = configuration.GetValue($"{Section}:SessionDays", 30);
        long maxFogBytes = configuration.GetValue($"{Section}:MaxFogBytes", 1024L * 1024L);

        if (sessionDays <= 0)
        {
            throw new InvalidOperationException($"{Section}:SessionDays must be positive.");
        }

        if (maxFogBytes <= 0)
        {
            throw new InvalidOperationException($"{Section}:MaxFogBytes must be positive.");
        }

        return new LocalAuthenticationOptions(
            identityPath,
            keyPath,
            configuration[$"{Section}:CookieName"]?.Trim() ?? "Polar.Factograph.Session",
            defaultRole,
            defaultCassetteId,
            configuration.GetValue($"{Section}:RegistrationEnabled", true),
            sessionDays,
            maxFogBytes);
    }

    private static string ResolvePath(string path, string contentRootPath) =>
        Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, contentRootPath);
}
