using System.Text;
using System.Text.RegularExpressions;
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
    ICassetteDocumentWriter documentWriter)
{
    private static readonly Regex LoginPattern = new(
        "^[a-z0-9][a-z0-9._-]{2,62}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);
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
            throw new InvalidOperationException("Registration is disabled.");
        }

        string normalizedLogin = NormalizeLogin(login);
        RequirePassword(password);
        await _registrationLock.WaitAsync(cancellationToken);
        try
        {
            if (store.FindByNormalizedLogin(normalizedLogin) is not null)
            {
                throw new ArgumentException("This login is already registered.", nameof(login));
            }

            string projectPath = projectPathResolver.GetRequiredPath();
            ProjectDefinition project = await projectLoader.LoadAsync(projectPath, cancellationToken);
            CassetteDefinition cassette = ResolveRegistrationCassette(project);
            if (!project.Roles.ContainsKey(options.DefaultRole))
            {
                throw new InvalidOperationException(
                    $"Default registration role '{options.DefaultRole}' is absent from the project.");
            }

            string userId = $"u_{Guid.NewGuid():N}";
            IdentityFogReference fog = await CreateFogAsync(
                cassette,
                userId,
                normalizedLogin.ToLowerInvariant(),
                cancellationToken);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            IdentityUser user = new()
            {
                Id = userId,
                Login = normalizedLogin.ToLowerInvariant(),
                NormalizedLogin = normalizedLogin,
                DisplayName = string.IsNullOrWhiteSpace(displayName)
                    ? normalizedLogin.ToLowerInvariant()
                    : displayName.Trim(),
                PasswordHash = string.Empty,
                Roles = [options.DefaultRole],
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
                DeleteFog(cassette, fog.RelativePath);
                throw;
            }

            return new LocalAuthenticationSession(user, device);
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
        string normalizedLogin = NormalizeLogin(login);
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

    public Task RevokeDeviceAsync(
        string userId,
        string deviceId,
        CancellationToken cancellationToken = default) =>
        store.UpdateAsync(current => current with
        {
            Devices = current.Devices.Select(device =>
                device.Id == deviceId && device.UserId == userId
                    ? device with { RevokedAtUtc = DateTimeOffset.UtcNow }
                    : device).ToArray()
        }, cancellationToken);

    public Task RevokeAllAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        store.UpdateAsync(current => current with
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
        Name = string.IsNullOrWhiteSpace(deviceName) ? "Browser" : deviceName.Trim(),
        CreatedAtUtc = now,
        LastSeenAtUtc = now,
        ExpiresAtUtc = now.AddDays(options.SessionDays)
    };

    private CassetteDefinition ResolveRegistrationCassette(ProjectDefinition project)
    {
        CassetteDefinition[] writable = project.Cassettes
            .Where(cassette => cassette.Enabled && cassette.AllowWrite)
            .ToArray();
        if (!string.IsNullOrWhiteSpace(options.DefaultCassetteId))
        {
            return writable.SingleOrDefault(cassette => cassette.Id == options.DefaultCassetteId)
                ?? throw new InvalidOperationException(
                    $"Registration cassette '{options.DefaultCassetteId}' is not enabled for writing.");
        }

        return writable.Length == 1
            ? writable[0]
            : throw new InvalidOperationException(
                "Authentication:Local:DefaultCassetteId is required when the project has multiple writable cassettes.");
    }

    private async Task<IdentityFogReference> CreateFogAsync(
        CassetteDefinition cassette,
        string userId,
        string login,
        CancellationToken cancellationToken)
    {
        byte[] content = CreateFogXml(userId, login);
        await using MemoryStream stream = new(content, writable: false);
        CassetteDocumentWriteResult result = await documentWriter.AddAsync(
            cassette,
            stream,
            $"{login}.fog",
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

    private static byte[] CreateFogXml(string userId, string login)
    {
        StringBuilder text = new();
        using XmlWriter writer = XmlWriter.Create(text, new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = Encoding.UTF8,
            Indent = true
        });
        writer.WriteStartElement("rdf", "RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        writer.WriteAttributeString("dbid", userId);
        writer.WriteAttributeString("owner", userId);
        writer.WriteAttributeString("prefix", login.Replace('.', '_').Replace('-', '_') + "_");
        writer.WriteAttributeString("counter", "1000");
        writer.WriteEndElement();
        writer.Flush();
        return Encoding.UTF8.GetBytes(text.ToString());
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

    private static string NormalizeLogin(string login)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        string normalized = login.Trim().ToUpperInvariant();
        if (!LoginPattern.IsMatch(normalized.ToLowerInvariant()))
        {
            throw new ArgumentException(
                "Login must contain 3-63 lowercase Latin letters, digits, dots, underscores, or hyphens.",
                nameof(login));
        }

        return normalized;
    }

    private static void RequirePassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        if (password.Length < 10)
        {
            throw new ArgumentException(
                "Password must contain at least 10 characters.",
                nameof(password));
        }
    }
}
