using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.SignalR;

namespace SceneSplit.Api.Interceptors;

public class GrpcErrorInterceptor(ILogger<GrpcErrorInterceptor> logger) : Interceptor
{
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);
        return new AsyncUnaryCall<TResponse>(
            HandleResponseAsync(call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponseAsync<TResponse>(Task<TResponse> inner)
    {
        try
        {
            return await inner;
        }
        catch (RpcException ex)
        {
            logger.LogError(ex, "gRPC call failed: {Status}", ex.StatusCode);
            throw new HubException("Compression service error!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC call failed: An unexpected error occurred.");
            throw new HubException("Internal error!");
        }
    }
}