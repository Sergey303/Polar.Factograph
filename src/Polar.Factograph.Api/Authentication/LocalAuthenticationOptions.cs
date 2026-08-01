namespace Polar.Factograph.Api.Authentication;

public sealed record LocalAuthenticationOptions(
    string IdentityPath,
    string DataProtectionKeysPath,
    string CookieName,
    string DefaultCassetteId,
    bool RegistrationEnabled,
    int SessionDays,
    long MaxFogBytes,
    IReadOnlySet<string> EditorLogins)
{
    public const string PublicViewerRole = "viewer";
    public const string DefaultPublicUserId = "$public";

    private const string Section = "Authentication:Local";

    public bool PublicReadEnabled { get; init; }

    public string PublicUserId { get; init; } = DefaultPublicUserId;

    public IReadOnlySet<string> AdminLogins { get; init; } =
        new HashSet<string>(StringComparer.Ordinal);

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
        int sessionDays = configuration.GetValue($"{Section}:SessionDays", 30);
        long maxFogBytes = configuration.GetValue($"{Section}:MaxFogBytes", 1024L * 1024L);
        bool publicReadEnabled = configuration.GetValue($"{Section}:PublicReadEnabled", false);
        string publicUserId = configuration[$"{Section}:PublicUserId"]?.Trim()
            ?? DefaultPublicUserId;
        HashSet<string> editorLogins = ReadLoginSet(configuration, "EditorLogins");
        HashSet<string> adminLogins = ReadLoginSet(configuration, "AdminLogins");

        if (sessionDays <= 0)
        {
            throw new InvalidOperationException($"{Section}:SessionDays must be positive.");
        }

        if (maxFogBytes <= 0)
        {
            throw new InvalidOperationException($"{Section}:MaxFogBytes must be positive.");
        }

        if (publicReadEnabled && string.IsNullOrWhiteSpace(publicUserId))
        {
            throw new InvalidOperationException(
                $"{Section}:PublicUserId must be configured when public reading is enabled.");
        }

        return new LocalAuthenticationOptions(
            identityPath,
            keyPath,
            configuration[$"{Section}:CookieName"]?.Trim() ?? "Polar.Factograph.Session",
            string.Empty,
            configuration.GetValue($"{Section}:RegistrationEnabled", true),
            sessionDays,
            maxFogBytes,
            editorLogins)
        {
            PublicReadEnabled = publicReadEnabled,
            PublicUserId = publicUserId,
            AdminLogins = adminLogins
        };
    }

    public bool IsEditor(string normalizedLogin) =>
        EditorLogins.Contains(normalizedLogin) || AdminLogins.Contains(normalizedLogin);

    public bool IsAdministrator(string normalizedLogin) => AdminLogins.Contains(normalizedLogin);

    public bool IsPublicUser(string userId) =>
        PublicReadEnabled && string.Equals(PublicUserId, userId, StringComparison.Ordinal);

    private static HashSet<string> ReadLoginSet(
        IConfiguration configuration,
        string settingName)
    {
        string[] values = configuration
            .GetSection($"{Section}:{settingName}")
            .Get<string[]>()
            ?? Array.Empty<string>();
        HashSet<string> normalizedLogins = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            string normalized = LocalLoginName.Normalize(value);
            if (!normalizedLogins.Add(normalized))
            {
                throw new InvalidOperationException(
                    $"{Section}:{settingName} contains duplicate login '{value}'.");
            }
        }

        return normalizedLogins;
    }

    private static string ResolvePath(string path, string contentRootPath) =>
        Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(path, contentRootPath);
}
