namespace SceneSplit.Api.Extenstions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddResilienceHandler(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler();
        return builder;
    }
}