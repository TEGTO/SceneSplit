using Microsoft.Extensions.Http.Resilience;

namespace SceneSplit.Api.Extenstions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddResilienceHandler(this IHttpClientBuilder builder,
        Action<HttpStandardResilienceOptions>? configureOptions = null)
    {
        builder.AddStandardResilienceHandler(configureOptions ?? (_ => { }));
        return builder;
    }
}