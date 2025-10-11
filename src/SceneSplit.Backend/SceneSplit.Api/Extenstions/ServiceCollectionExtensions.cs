using SceneSplit.Configuration;

namespace SceneSplit.Api.Extenstions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration, string allowSpecificOrigins, bool isDevelopment)
    {
        var allowedOriginsString = configuration[ApiConfigurationKeys.ALLOWED_CORS_ORIGINS] ?? string.Empty;
        var allowedOrigins = allowedOriginsString.Split(",", StringSplitOptions.RemoveEmptyEntries);

        services.AddCors(options =>
        {
            options.AddPolicy(name: allowSpecificOrigins, policy =>
            {
                if (allowedOrigins.Contains("*"))
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .AllowAnyMethod();
                }

                if (isDevelopment)
                {
                    policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");
                }
            });
        });

        return services;
    }
}