using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace SceneSplit.GrpcClientShared.Interceptors;

public class GrpcResilienceInterceptor(ILogger logger) : Interceptor
{
    private readonly AsyncRetryPolicy retryPolicy = Policy
        .Handle<RpcException>(ex =>
            ex.StatusCode is StatusCode.Unavailable or
            StatusCode.DeadlineExceeded or
            StatusCode.Internal or
            StatusCode.ResourceExhausted)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
                TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
            onRetry: (exception, timeSpan, retryCount, context) =>
            {
                var rpcEx = (RpcException)exception;
                logger.LogWarning("Retrying gRPC call (attempt {RetryCount}) after {Delay} due to {StatusCode}: {Message}",
                    retryCount, timeSpan, rpcEx.StatusCode, rpcEx.Message);
            });

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            ExecuteWithResilience(call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> ExecuteWithResilience<TResponse>(Task<TResponse> innerCall)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await innerCall;
        });
    }
}