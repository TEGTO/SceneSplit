using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using SceneSplit.GrpcClientShared.Interceptors;

namespace SceneSplit.GrpcClientShared.Extenstions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcClientWeb<TClient>(this IServiceCollection services,
        string uri, Action<HttpStandardResilienceOptions>? configureHttpResilienceOptions = null)
        where TClient : class
    {
        services.AddTransient<GrpcErrorInterceptor>();
        services.AddTransient<GrpcResilienceInterceptor>();

        services.AddGrpcClient<TClient>(o =>
        {
            o.Address = new Uri(uri);
        })
        .AddInterceptor<GrpcErrorInterceptor>()
        .AddInterceptor<GrpcResilienceInterceptor>()
        .AddResilienceHandler(configureHttpResilienceOptions)
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
#pragma warning disable S4830 // It's okay for a pet project
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
#pragma warning restore S4830

            return new GrpcWebHandler(GrpcWebMode.GrpcWeb, handler);
        });

        return services;
    }

    private static IHttpClientBuilder AddResilienceHandler(this IHttpClientBuilder builder,
        Action<HttpStandardResilienceOptions>? configureOptions = null)
    {
        builder.AddStandardResilienceHandler(configureOptions ?? (_ => { }));
        return builder;
    }
}