using Moq;
using Moq.Protected;
using SceneSplit.GrpcClientShared.DelegatingHandlers;
using System.Net;

namespace SceneSplit.GrpcClientShared.UnitTests.DelegatingHandlers;

[TestFixture]
public class Http11EnforcerHandlerTests
{
    [Test]
    public async Task SendAsync_Request_EnforcesHttp11Version()
    {
        // Arrange
        var innerHandlerMock = new Mock<HttpMessageHandler>();

        HttpRequestMessage? capturedRequest = null;

        innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var handler = new Http11EnforcerHandler
        {
            InnerHandler = innerHandlerMock.Object
        };

        var httpClient = new HttpClient(handler);

        // Act
        await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://localhost"));

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Version, Is.EqualTo(HttpVersion.Version11));
        Assert.That(capturedRequest.VersionPolicy, Is.EqualTo(HttpVersionPolicy.RequestVersionExact));
    }

    [Test]
    public async Task SendAsync_DelegatesToInnerHandler_ReturnsInnerResponse()
    {
        // Arrange
        var expectedResponse = new HttpResponseMessage(HttpStatusCode.Accepted);

        var innerHandlerMock = new Mock<HttpMessageHandler>();
        innerHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = new Http11EnforcerHandler
        {
            InnerHandler = innerHandlerMock.Object
        };

        var httpClient = new HttpClient(handler);

        // Act
        var actualResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://localhost"));

        // Assert
        Assert.That(actualResponse, Is.EqualTo(expectedResponse));
    }
}