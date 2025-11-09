using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using SceneSplit.GrpcClientShared.DelegatingHandlers;
using SceneSplit.GrpcClientShared.Interceptors;

namespace SceneSplit.GrpcClientShared.Helpers;

public static class GrpcClientFactoryHelper
{
    public static TClient CreateGrpcClientWeb<TClient>(
        string uri,
        ILogger<GrpcErrorInterceptor> errorLogger,
        ILogger<GrpcResilienceInterceptor> resilienceLogger,
        Action<HttpStandardResilienceOptions>? configureHttpResilienceOptions = null,
        Action<GrpcChannelOptions>? configureChannelOptions = null)
        where TClient : class
    {
        var httpHandler = new HttpClientHandler
        {
            UseProxy = false
        };

        var enforcedHandler = new Http11EnforcerHandler
        {
            InnerHandler = httpHandler
        };

        var grpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, enforcedHandler);

        var resilienceHandler = CreateResilienceHandler(configureHttpResilienceOptions);

        resilienceHandler.InnerHandler = grpcWebHandler;

        var channelOptions = new GrpcChannelOptions
        {
            HttpHandler = resilienceHandler
        };

        configureChannelOptions?.Invoke(channelOptions);

        var channel = GrpcChannel.ForAddress(uri, channelOptions);

        var callInvoker = channel.Intercept(
            new GrpcResilienceInterceptor(resilienceLogger),
            new GrpcErrorInterceptor(errorLogger)
        );

        var interceptedClient = (TClient)Activator.CreateInstance(typeof(TClient), callInvoker)!;
        return interceptedClient;
    }

    private static PolicyHttpMessageHandler CreateResilienceHandler(Action<HttpStandardResilienceOptions>? configureOptions)
    {
        var options = new HttpStandardResilienceOptions();
        configureOptions?.Invoke(options);

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(options.Retry.MaxRetryAttempts, retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .AdvancedCircuitBreakerAsync(
                failureThreshold: options.CircuitBreaker.FailureRatio,
                samplingDuration: options.CircuitBreaker.SamplingDuration,
                minimumThroughput: options.CircuitBreaker.MinimumThroughput,
                durationOfBreak: options.CircuitBreaker.BreakDuration);

        return new PolicyHttpMessageHandler(Policy.WrapAsync(retryPolicy, circuitBreakerPolicy));
    }
}