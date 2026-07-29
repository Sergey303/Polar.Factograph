using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Identity;
using Polar.Factograph.Api.Infrastructure;
using Polar.Factograph.Application;
using Polar.Factograph.Domain;
using Polar.Factograph.Fog;

namespace Polar.Factograph.Api.Authentication;

public sealed class LocalAuthenticationService(
    IdentityJsonStore store,
    IPasswordHasher<IdentityUser> passwordHasher,
    LocalAuthenticationOptions options,
    ProjectPathResolver projectPathResolver,
    ProjectConfigurationLoader projectLoader,
    ICassetteNamedFogWriter fogWriter,
    ILogger<LocalAuthenticationService> logger)
{
    private const string ViewerRole = "viewer";
    private const string EditorRole = "editor";
    private readonly SemaphoreSlim _registrationLock = new(1, 1);

    public async Task<LocalAuthenticationSession> RegisterAsync(
        string login,
        string password,
        string? displayName,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        if (!options.RegistrationEnabled)
        {
            throw new InvalidOperationException("Регистрация отключена.");
        }

        string canonicalLogin = LocalLoginName.Canonicalize(login);
        string normalizedLogin = LocalLoginName.NormalizeCanonical(canonicalLogin);
        RequirePassword(password);
        await _registrationLock.WaitAsync(cancellationToken);
        try
        {
            if (store.FindByNormalizedLogin(normalizedLogin) is not null)
            {
                throw new ArgumentException("Этот логин уже зарегистрирован.", nameof(login));
            }

            string projectPath = projectPathResolver.GetRequiredPath();
            ProjectDefinition project = await projectLoader.LoadAsync(projectPath, cancellationToken);
            bool editor = options.IsEditor(normalizedLogin);
            string role = editor ? EditorRole : ViewerRole;
            RequireProjectRole(project, role);

            string userId = $"u_{Guid.NewGuid():N}";
            CassetteDefinition? cassette = null;
            IdentityFogReference? fog = null;
            if (editor)
            {
                cassette = ResolveRegistrationCassette(project);
                fog = await CreateFogAsync(
                    cassette,
                    userId,
                    canonicalLogin,
                    cancellationToken);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            IdentityUser user = new()
            {
                Id = userId,
                Login = canonicalLogin,
                NormalizedLogin = normalizedLogin,
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? canonicalLogin
                    : displayName.Trim(),
                PasswordHash = string.Empty,
                Roles = [role],
                Fog = fog,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            user = user with
            {
                PasswordHash = passwordHasher.HashPassword(user, password)
            };
            IdentityDevice device = CreateDevice(user.Id, deviceName, now);

            try
            {
                await store.UpdateAsync(current => current with
                {
                    Users = [.. current.Users, user],
                    Devices = [.. current.Devices, device]
                }, cancellationToken);
            }
            catch
            {
                if (cassette is not null && fog is not null)
                {
                    DeleteFog(cassette, fog.RelativePath);
                }
                throw;
            }

            return new LocalAuthenticationSession(user, device);
        }
        finally
        {
            _registrationLock.Release();
        }
    }

    public async Task ProvisionConfiguredEditorsAsync(
        CancellationToken cancellationToken = default)
    {
        await _registrationLock.WaitAsync(cancellationToken);
        try
        {
            string projectPath = projectPathResolver.GetRequiredPath();
            ProjectDefinition project = await projectLoader.LoadAsync(projectPath, cancellationToken);
            RequireProjectRole(project, ViewerRole);
            RequireProjectRole(project, EditorRole);

            Dictionary<string, IdentityUser> replacements = new(StringComparer.Ordinal);
            List<IdentityFogReference> createdFogs = [];
            HashSet<string> registeredLogins = store.Current.Users
                .Select(user => user.NormalizedLogin)
                .ToHashSet(StringComparer.Ordinal);
            CassetteDefinition? cassette = null;

            foreach (IdentityUser user in store.Current.Users)
            {
                bool editor = options.IsEditor(user.NormalizedLogin);
                string[] desiredRoles = [editor ? EditorRole : ViewerRole];
                IdentityFogReference? desiredFog = editor ? user.Fog : null;

                if (editor)
                {
                    cassette ??= ResolveRegistrationCassette(project);
                    if (!FogIsUsable(cassette, desiredFog))
                    {
                        desiredFog = await CreateFogAsync(
                            cassette,
                            user.Id,
                            user.Login,
                            cancellationToken);
                        createdFogs.Add(desiredFog);
                    }
                }

                if (!user.Roles.SequenceEqual(desiredRoles, StringComparer.Ordinal) ||
                    !Equals(user.Fog, desiredFog))
                {
                    replacements[user.Id] = user with
                    {
                        Roles = desiredRoles,
                        Fog = desiredFog,
                        UpdatedAtUtc = DateTimeOffset.UtcNow
                    };
                }
            }

            try
            {
                if (replacements.Count > 0)
                {
                    await store.UpdateAsync(current => current with
                    {
                        Users = current.Users
                            .Select(user => replacements.TryGetValue(user.Id, out IdentityUser? replacement)
                                ? replacement
                                : user)
                            .ToArray()
                    }, cancellationToken);
                }
            }
            catch
            {
                if (cassette is not null)
                {
                    foreach (IdentityFogReference fog in createdFogs)
                    {
                        DeleteFog(cassette, fog.RelativePath);
                    }
                }
                throw;
            }

            foreach (string normalizedLogin in options.EditorLogins)
            {
                if (!registeredLogins.Contains(normalizedLogin))
                {
                    logger.LogWarning(
                        "Configured editor login '{EditorLogin}' is not registered yet; its Fog will be created after registration or on the next application start.",
                        normalizedLogin);
                }
            }

            logger.LogInformation(
                "Reconciled local users with the editor login list. Editors: {EditorCount}; updated users: {UpdatedCount}.",
                options.EditorLogins.Count,
                replacements.Count);
        }
        finally
        {
            _registrationLock.Release();
        }
    }

    public async Task<LocalAuthenticationSession?> LoginAsync(
        string login,
        string password,
        string? deviceName,
        CancellationToken cancellationToken = default)
    {
        string normalizedLogin = LocalLoginName.Normalize(login);
        IdentityUser? user = store.FindByNormalizedLogin(normalizedLogin);
        if (user is null || !user.Enabled)
        {
            return null;
        }

        PasswordVerificationResult verification = passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        IdentityUser updatedUser = verification == PasswordVerificationResult.SuccessRehashNeeded
            ? user with
            {
                PasswordHash = passwordHasher.HashPassword(user, password),
                UpdatedAtUtc = now
            }
            : user;
        IdentityDevice device = CreateDevice(user.Id, deviceName, now);

        IdentityData updated = await store.UpdateAsync(current => current with
        {
            Users = current.Users
                .Select(value => value.Id == updatedUser.Id ? updatedUser : value)
                .ToArray(),
            Devices = [.. current.Devices, device]
        }, cancellationToken);
        return new LocalAuthenticationSession(
            updated.Users.Single(value => value.Id == user.Id),
            device);
    }

    public LocalAuthenticationSession? ResolveSession(string userId, string deviceId)
    {
        IdentityUser? user = store.FindUser(userId);
        IdentityDevice? device = store.FindDevice(deviceId);
        if (user is null || device is null || !user.Enabled || device.UserId != user.Id)
        {
            return null;
        }

        if (device.RevokedAtUtc is not null || device.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        return new LocalAuthenticationSession(user, device);
    }

    public async Task RevokeDeviceAsync(
        string userId,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        _ = await store.UpdateAsync(current => current with
        {
            Devices = current.Devices.Select(device =>
                device.Id == deviceId && device.UserId == userId
                    ? device with { RevokedAtUtc = DateTimeOffset.UtcNow }
                    : device).ToArray()
        }, cancellationToken);
    }

    public async Task RevokeAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _ = await store.UpdateAsync(current => current with
        {
            Users = current.Users.Select(user => user.Id == userId
                ? user with
                {
                    SecurityVersion = checked(user.SecurityVersion + 1),
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                }
                : user).ToArray(),
            Devices = current.Devices.Select(device => device.UserId == userId
                ? device with { RevokedAtUtc = DateTimeOffset.UtcNow }
                : device).ToArray()
        }, cancellationToken);
    }

    public IReadOnlyList<IdentityDevice> GetDevices(string userId) => store.Current.Devices
        .Where(device => device.UserId == userId)
        .OrderByDescending(device => device.LastSeenAtUtc)
        .ToArray();

    private IdentityDevice CreateDevice(
        string userId,
        string? deviceName,
        DateTimeOffset now) => new()
    {
        Id = $"d_{Guid.NewGuid():N}",
        UserId = userId,
        Name = string.IsNullOrWhiteSpace(deviceName) ? "Браузер" : deviceName.Trim(),
        CreatedAtUtc = now,
        LastSeenAtUtc = now,
        ExpiresAtUtc = now.AddDays(options.SessionDays)
    };

    private static void RequireProjectRole(ProjectDefinition project, string role)
    {
        if (!project.Roles.ContainsKey(role))
        {
            throw new InvalidOperationException(
                $"Роль локальной аутентификации '{role}' отсутствует в проекте.");
        }
    }

    private CassetteDefinition ResolveRegistrationCassette(ProjectDefinition project)
    {
        CassetteDefinition[] writable = project.Cassettes
            .Where(cassette => cassette.Enabled && cassette.AllowWrite)
            .ToArray();
        if (!string.IsNullOrWhiteSpace(options.DefaultCassetteId))
        {
            return writable.SingleOrDefault(cassette => cassette.Id == options.DefaultCassetteId)
                ?? throw new InvalidOperationException(
                    $"Кассета регистрации '{options.DefaultCassetteId}' не найдена или недоступна для записи.");
        }

        return writable.Length == 1
            ? writable[0]
            : throw new InvalidOperationException(
                "Укажите Authentication:Local:DefaultCassetteId, когда для записи доступно несколько кассет.");
    }

    private static bool FogIsUsable(
        CassetteDefinition cassette,
        IdentityFogReference? fog)
    {
        if (fog is null ||
            !string.Equals(fog.CassetteId, cassette.Id, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            string path = Path.GetFullPath(fog.RelativePath, cassette.Path);
            return File.Exists(path);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    private async Task<IdentityFogReference> CreateFogAsync(
        CassetteDefinition cassette,
        string userId,
        string login,
        CancellationToken cancellationToken)
    {
        byte[] content = CreateFogXml(userId);
        await using MemoryStream stream = new(content, writable: false);
        CassetteDocumentWriteResult result = await fogWriter.AddAsync(
            cassette,
            stream,
            LocalLoginName.ToFogFileName(login),
            options.MaxFogBytes,
            cancellationToken);
        return new IdentityFogReference
        {
            CassetteId = result.CassetteId,
            DocumentUri = result.DocumentUri,
            RelativePath = Path.Combine(
                    "originals",
                    result.FolderName,
                    result.FileName)
                .Replace('\\', '/')
        };
    }

    private static byte[] CreateFogXml(string userId)
    {
        using MemoryStream stream = new();
        using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true
        }))
        {
            writer.WriteStartElement(
                "rdf",
                "RDF",
                "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            writer.WriteAttributeString("dbid", userId);
            writer.WriteAttributeString("owner", userId);
            writer.WriteAttributeString("prefix", userId + "_");
            writer.WriteAttributeString("counter", "1000");
            writer.WriteEndElement();
        }

        return stream.ToArray();
    }

    private static void DeleteFog(CassetteDefinition cassette, string relativePath)
    {
        try
        {
            string path = Path.GetFullPath(relativePath, cassette.Path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // An orphaned empty Fog is safer than hiding the identity write failure.
        }
    }

    private static void RequirePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Введите пароль.", nameof(password));
        }

        if (password.Length < 10)
        {
            throw new ArgumentException(
                "Пароль должен содержать не менее 10 символов.",
                nameof(password));
        }
    }
}
