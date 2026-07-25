using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Polar.Factograph.Api.Authentication;

public static class FactographAuthentication
{
    public static IServiceCollection AddFactographAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        JwtAuthenticationSettings? settings = JwtAuthenticationSettings.Read(configuration);
        if (settings is null)
        {
            services.AddAuthentication();
        }
        else
        {
            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = settings.Authority;
                    options.Audience = settings.Audience;
                    options.RequireHttpsMetadata = settings.RequireHttpsMetadata;
                    options.MapInboundClaims = false;
                });
        }

        services.AddAuthorization();
        return services;
    }
}
