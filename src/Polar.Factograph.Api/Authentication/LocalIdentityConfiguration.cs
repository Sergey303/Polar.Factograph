using Microsoft.Extensions.FileProviders;

namespace Polar.Factograph.Api.Authentication;

public static class LocalIdentityConfiguration
{
    public static LocalAuthenticationOptions AddLocalIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        LocalAuthenticationOptions options = LocalAuthenticationOptions.Read(
            configuration,
            environment);

        string directory = Path.GetDirectoryName(options.IdentityPath)
            ?? throw new InvalidOperationException("Identity path has no parent directory.");
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(options.DataProtectionKeysPath);

        PhysicalFileProvider provider = new(directory);
        IConfigurationRoot identityConfiguration = new ConfigurationBuilder()
            .AddJsonFile(
                provider,
                Path.GetFileName(options.IdentityPath),
                optional: true,
                reloadOnChange: true)
            .Build();

        services.AddSingleton(options);
        services.AddSingleton(identityConfiguration);
        services.Configure<IdentityData>(identityConfiguration);
        return options;
    }
}
