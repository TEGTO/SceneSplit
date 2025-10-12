using Grpc.Net.Client.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using SceneSplit.GrpcClientShared.Interceptors;
using System.Net;

namespace SceneSplit.GrpcClientShared.Extenstions;

internal sealed class Http11EnforcerHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        return base.SendAsync(request, cancellationToken);
    }
}

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