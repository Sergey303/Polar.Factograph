using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Polar.Factograph.Api.Authentication;

public sealed class IdentityJsonStore : IDisposable
{
    private const int PublishAttempts = 8;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly LocalAuthenticationOptions _options;
    private readonly ILogger<IdentityJsonStore> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly IDisposable? _reloadSubscription;
    private IdentityData _current;

    public IdentityJsonStore(
        IOptionsMonitor<IdentityData> monitor,
        LocalAuthenticationOptions options,
        ILogger<IdentityJsonStore> logger)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        _options = options;
        _logger = logger;
        _current = Validate(monitor.CurrentValue);
        _reloadSubscription = monitor.OnChange(Reload);
    }

    public IdentityData Current => Volatile.Read(ref _current);

    public IdentityUser? FindUser(string userId) => Current.Users.FirstOrDefault(
        user => string.Equals(user.Id, userId, StringComparison.Ordinal));

    public IdentityUser? FindByNormalizedLogin(string normalizedLogin) =>
        Current.Users.FirstOrDefault(user => string.Equals(
            user.NormalizedLogin,
            normalizedLogin,
            StringComparison.Ordinal));

    public IdentityDevice? FindDevice(string deviceId) => Current.Devices.FirstOrDefault(
        device => string.Equals(device.Id, deviceId, StringComparison.Ordinal));

    public async Task<IdentityData> UpdateAsync(
        Func<IdentityData, IdentityData> update,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            IdentityData next = Validate(update(Current));
            await WriteAsync(next, cancellationToken);
            Volatile.Write(ref _current, next);
            return next;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        _reloadSubscription?.Dispose();
        _writeLock.Dispose();
    }

    private void Reload(IdentityData value)
    {
        try
        {
            IdentityData validated = Validate(value);
            Volatile.Write(ref _current, validated);
            _logger.LogInformation("Reloaded local identity data from JSON.");
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            _logger.LogError(
                exception,
                "Ignored invalid local identity JSON and kept the previous snapshot.");
        }
    }

    private async Task WriteAsync(
        IdentityData value,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(_options.IdentityPath)!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(_options.IdentityPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    value,
                    JsonOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            await PublishTemporaryFileAsync(
                temporaryPath,
                _options.IdentityPath,
                cancellationToken);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    private static async Task PublishTemporaryFileAsync(
        string temporaryPath,
        string targetPath,
        CancellationToken cancellationToken)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(temporaryPath, targetPath, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < PublishAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A stale temporary file is harmless and can be cleaned up later.
        }
        catch (UnauthorizedAccessException)
        {
            // Do not hide the original write error with a cleanup failure.
        }
    }

    private static IdentityData Validate(IdentityData value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported identity schema version: {value.SchemaVersion}.");
        }

        IdentityUser[] users = value.Users
            ?? throw new InvalidDataException("Identity users cannot be null.");
        IdentityDevice[] devices = value.Devices
            ?? throw new InvalidDataException("Identity devices cannot be null.");

        RequireUnique(users.Select(user => user.Id), "user id");
        RequireUnique(users.Select(user => user.NormalizedLogin), "normalized login");
        RequireUnique(devices.Select(device => device.Id), "device id");
        HashSet<string> userIds = users
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (IdentityUser user in users)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Id);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Login);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.NormalizedLogin);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.PasswordHash);
            ArgumentNullException.ThrowIfNull(user.Fog);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Fog.CassetteId);
            ArgumentException.ThrowIfNullOrWhiteSpace(user.Fog.RelativePath);
        }

        foreach (IdentityDevice device in devices)
        {
            if (!userIds.Contains(device.UserId))
            {
                throw new InvalidDataException(
                    $"Device '{device.Id}' refers to unknown user '{device.UserId}'.");
            }
        }

        return value with
        {
            Users = users,
            Devices = devices
        };
    }

    private static void RequireUnique(IEnumerable<string> values, string name)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (!seen.Add(value))
            {
                throw new InvalidDataException($"Duplicate {name}: '{value}'.");
            }
        }
    }
}
