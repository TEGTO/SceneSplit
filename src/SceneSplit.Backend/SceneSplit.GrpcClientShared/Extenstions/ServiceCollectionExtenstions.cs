using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using SceneSplit.GrpcClientShared.DelegatingHandlers;
using SceneSplit.GrpcClientShared.Interceptors;

namespace SceneSplit.GrpcClientShared.Extenstions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGrpcClientWeb<TClient>(this IServiceCollection services,
        string uri, Action<HttpStandardResilienceOptions>? configureHttpResilienceOptions = null,
        Action<IServiceProvider, GrpcChannelOptions>? configureChannelOptions = null)
        where TClient : class
    {
        services.AddTransient<GrpcErrorInterceptor>();
        services.AddTransient<GrpcResilienceInterceptor>();

        services.AddGrpcClient<TClient>(o =>
        {
            o.Address = new Uri(uri);
        })
        .ConfigureChannel((provider, options) =>
        {
            configureChannelOptions?.Invoke(provider, options);
        })
        .AddHttpMessageHandler(() => new Http11EnforcerHandler())
        .AddInterceptor<GrpcErrorInterceptor>()
        .AddInterceptor<GrpcResilienceInterceptor>()
        .AddResilienceHandler(configureHttpResilienceOptions)
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var httpClientHandler = new HttpClientHandler
            {
                UseProxy = false
            };

            return new GrpcWebHandler(GrpcWebMode.GrpcWeb, httpClientHandler);
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