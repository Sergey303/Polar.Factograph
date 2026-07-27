using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Polar.Factograph.Api.Authentication;

public static class FactographAuthentication
{
    public static IServiceCollection AddFactographAuthentication(
        this IServiceCollection services,
        LocalAuthenticationOptions settings,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(settings.DataProtectionKeysPath));
        services.AddSingleton<IPasswordHasher<IdentityUser>, PasswordHasher<IdentityUser>>();
        services.AddScoped<LocalCookieValidator>();
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = settings.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromDays(settings.SessionDays);
                options.SlidingExpiration = false;
                options.EventsType = typeof(LocalCookieValidator);
            });
        services.AddAuthorization();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = $"{settings.CookieName}.Antiforgery";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
        });
        return services;
    }
}
