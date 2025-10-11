using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.GrpcClientShared.Interceptors;
using static Grpc.Core.Interceptors.Interceptor;

namespace SceneSplit.GrpcClientShared.UnitTests.Interceptors;

[TestFixture]
public class GrpcErrorInterceptorTests
{
    private Mock<ILogger<GrpcErrorInterceptor>> mockLogger;

    private GrpcErrorInterceptor interceptor;

    [SetUp]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<GrpcErrorInterceptor>>();

        interceptor = new GrpcErrorInterceptor(mockLogger.Object);
    }

    [Test]
    public async Task AsyncUnaryCall_WhenCallSucceeds_ReturnsExpectedResponse()
    {
        // Arrange
        var expectedResponse = "success";
        var request = new object();
        var context = CreateFakeContext<object, string>();

        AsyncUnaryCallContinuation<object, string> continuation =
            (req, ctx) => new AsyncUnaryCall<string>(
                Task.FromResult(expectedResponse),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });

        // Act
        var resultCall = interceptor.AsyncUnaryCall(request, context, continuation);
        var result = await resultCall.ResponseAsync;

        // Assert
        Assert.That(result, Is.EqualTo(expectedResponse));
        mockLogger.VerifyNoOtherCalls();
    }

    [Test]
    public void AsyncUnaryCall_WhenRpcExceptionThrown_ThrowsHubExceptionWithCompressionMessage()
    {
        // Arrange
        var rpcException = new RpcException(new Status(StatusCode.Internal, "gRPC failure"));
        var request = new object();
        var context = CreateFakeContext<object, string>();

        AsyncUnaryCallContinuation<object, string> continuation =
            (req, ctx) => new AsyncUnaryCall<string>(
                Task.FromException<string>(rpcException),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });

        // Act & Assert
        var call = interceptor.AsyncUnaryCall(request, context, continuation);

        var ex = Assert.ThrowsAsync<HubException>(async () => await call.ResponseAsync);
        Assert.That(ex!.Message, Is.EqualTo("Compression service error!"));

        mockLogger.Verify(
            l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("gRPC call failed")),
                rpcException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void AsyncUnaryCall_WhenGeneralExceptionThrown_ThrowsHubExceptionWithInternalMessage()
    {
        // Arrange
        var generalEx = new InvalidOperationException("boom");
        var request = new object();
        var context = CreateFakeContext<object, string>();

        AsyncUnaryCallContinuation<object, string> continuation =
            (req, ctx) => new AsyncUnaryCall<string>(
                Task.FromException<string>(generalEx),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => [],
                () => { });

        // Act & Assert
        var call = interceptor.AsyncUnaryCall(request, context, continuation);

        var ex = Assert.ThrowsAsync<HubException>(async () => await call.ResponseAsync);
        Assert.That(ex!.Message, Is.EqualTo("Internal error!"));

        mockLogger.Verify(
            l => l.Log(
                It.Is<LogLevel>(lvl => lvl == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("unexpected error")),
                generalEx,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static ClientInterceptorContext<TRequest, TResponse> CreateFakeContext<TRequest, TResponse>()
        where TRequest : class
        where TResponse : class
    {
        var method = new Method<TRequest, TResponse>(
            MethodType.Unary,
            "FakeService",
            "FakeMethod",
            Marshallers.Create<TRequest>(_ => [], _ => default!),
            Marshallers.Create<TResponse>(_ => [], _ => default!));

        var host = "localhost";
        var callOptions = new CallOptions();
        return new ClientInterceptorContext<TRequest, TResponse>(method, host, callOptions);
    }
}