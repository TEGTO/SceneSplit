using SceneSplit.Configuration;

namespace SceneSplit.Api.Extenstions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCors(this IServiceCollection services, IConfiguration configuration, string allowSpecificOrigins, bool isDevelopment)
    {
        var allowedOriginsString = configuration[ApiConfigurationKeys.ALLOWED_CORS_ORIGINS] ?? string.Empty;
        var allowedOrigins = allowedOriginsString
            .Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        services.AddCors(options =>
        {
            options.AddPolicy(name: allowSpecificOrigins, policy =>
            {
                if (isDevelopment && (allowedOrigins.Length == 0 || allowedOrigins.Contains("*")))
                {
                    policy
                        .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var o) && o.Host == "localhost")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else if (allowedOrigins.Contains("*"))
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                }
                else
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }
}