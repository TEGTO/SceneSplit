using Amazon.Lambda.SQSEvents;
using Amazon.S3.Transfer;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.ImageCompression.Sdk;
using SceneSplit.SceneAnalysisLambda.Sdk;
using SceneSplit.TestShared;
using SceneSplit.TestShared.Helpers;
using System.Net;
using System.Text;
using System.Text.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SceneSplit.ObjectImageSearchLambda.UnitTests;

[TestFixture]
public class FunctionTests
{
    private Mock<ITransferUtility> transferMock = null!;
    private Mock<Compression.CompressionClient> compressionMock = null!;
    private Mock<ILogger<Function>> loggerMock = null!;

    private ObjectImageSearchLambdaOptions options = null!;

    [SetUp]
    public void SetUp()
    {
        transferMock = new Mock<ITransferUtility>();

        compressionMock = new Mock<Compression.CompressionClient>();

        loggerMock = TestHelper.CreateLoggerMock<Function>();

        options = new ObjectImageSearchLambdaOptions
        {
            BucketName = "bucket",
            ImageSearchApiEndpoint = "http://localhost/search/photos",
            ImageSearchApiKey = "test-key",
            CompressionApiUrl = "http://localhost:5163",
            MaxImageSize = 10_000_000,
            ResizeWidth = 800,
            ResizeHeight = 600,
            ImageQualityCompression = 75
        };
    }

    [Test]
    public async Task Handler_WhenNoImagesFound_LogsAndDoesNotUpload()
    {
        // Arrange
        var http = new HttpClient(new StubHandler(req =>
        {
            var json = """{ "results": [] }""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));

        var function = new Function(transferMock.Object, options, http, compressionMock.Object, loggerMock.Object);

        var message = new SceneAnalysisResult
        {
            WorkflowTags = [],
            Items = ["cat"]
        };
        var sqsEvent = CreateSqsEvent(JsonSerializer.Serialize(message));

        // Act
        await function.Handler(sqsEvent, Mock.Of<Amazon.Lambda.Core.ILambdaContext>());

        // Assert
        transferMock.Verify(t => t.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingMessage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.SearchingImages));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.NoImagesFound));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessedSuccessfully));
        loggerMock.VerifyLog(LogLevel.Information, Times.Never(), nameof(Log.UploadedToBucket));
    }

    [Test]
    public void Handler_WhenMessageIsInvalid_ThrowsAndLogs()
    {
        // Arrange
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
        var function = new Function(transferMock.Object, options, http, compressionMock.Object, loggerMock.Object);

        var sqsEvent = CreateSqsEvent("<<not json>>");

        // Act
        var ex = Assert.ThrowsAsync<JsonException>(async () =>
            await function.Handler(sqsEvent, Mock.Of<Amazon.Lambda.Core.ILambdaContext>()));

        // Assert
        Assert.That(ex, Is.Not.Null);
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingMessage));
        loggerMock.VerifyLog(LogLevel.Error, Times.Once(), nameof(Log.FailedToProcessWithException));
    }

    [Test]
    public void Handler_WhenSearchHttpFails_ThrowsAndLogs()
    {
        // Arrange
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        var function = new Function(transferMock.Object, options, http, compressionMock.Object, loggerMock.Object);

        var message = new SceneAnalysisResult
        {
            WorkflowTags = new Dictionary<string, string>(),
            Items = ["dog"]
        };
        var sqsEvent = CreateSqsEvent(JsonSerializer.Serialize(message));

        // Act
        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await function.Handler(sqsEvent, Mock.Of<Amazon.Lambda.Core.ILambdaContext>()));

        // Assert
        Assert.That(ex, Is.Not.Null);
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingMessage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.SearchingImages));
        loggerMock.VerifyLog(LogLevel.Error, Times.Once(), nameof(Log.FailedToProcess));
        loggerMock.VerifyLog(LogLevel.Information, Times.Never(), nameof(Log.ProcessedSuccessfully));
    }

    [Test]
    public async Task Handler_WhenOneImageFound_CompressesAndUploads_AndLogs()
    {
        // Arrange
        var searchJson = """
        {
          "results": [
            { "urls": { "regular": "http://images.local/1.jpg" } }
          ]
        }
        """;

        var http = new HttpClient(new StubHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.StartsWith(options.ImageSearchApiEndpoint))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
                };
            }

            // Image download
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([9, 9, 9])
            };
        }));

        var compressionReply = new CompressionReply
        {
            CompressedImage = ByteString.CopyFrom([1, 2, 3]),
            Format = "jpg",
            OriginalSize = 3,
            CompressedSize = 3
        };

        var asyncUnaryCall = GrpcTestHelpers.CreateAsyncUnaryCall(compressionReply);

        compressionMock
            .Setup(c => c.CompressImageAsync(
                It.IsAny<CompressionRequest>(),
                It.IsAny<Metadata>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        transferMock
            .Setup(t => t.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var function = new Function(transferMock.Object, options, http, compressionMock.Object, loggerMock.Object);

        var message = new SceneAnalysisResult
        {
            WorkflowTags = new Dictionary<string, string> { ["UserId"] = "u1" },
            Items = ["mountain"]
        };
        var sqsEvent = CreateSqsEvent(JsonSerializer.Serialize(message));

        // Act
        await function.Handler(sqsEvent, Mock.Of<Amazon.Lambda.Core.ILambdaContext>());

        // Assert
        transferMock.Verify(t => t.UploadAsync(
            It.Is<TransferUtilityUploadRequest>(r => r.BucketName == options.BucketName && r.ContentType.StartsWith("image/")),
            It.IsAny<CancellationToken>()), Times.Once);

        compressionMock.Verify(
            c => c.CompressImageAsync(
                It.IsAny<CompressionRequest>(),
                It.IsAny<Metadata>(),
                null,
                It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingMessage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.SearchingImages));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.DownloadingImage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.UploadedToBucket));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessedSuccessfully));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Never(), nameof(Log.NoImagesFound));
        loggerMock.VerifyLog(LogLevel.Error, Times.Never(), nameof(Log.FailedToProcess));
    }

    [Test]
    public async Task Handler_WhenMultipleImagesFound_UploadsEach_AndLogsCounts()
    {
        // Arrange
        var searchJson = """
        {
          "results": [
            { "urls": { "regular": "http://images.local/1.jpg" } },
            { "urls": { "regular": "http://images.local/2.jpg" } }
          ]
        }
        """;

        var http = new HttpClient(new StubHandler(req =>
        {
            if (req.RequestUri!.AbsoluteUri.StartsWith(options.ImageSearchApiEndpoint))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(searchJson, Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([7, 7, 7])
            };
        }));

        var compressionReply = new CompressionReply
        {
            CompressedImage = ByteString.CopyFrom([4, 5, 6]),
            Format = "jpeg",
            OriginalSize = 3,
            CompressedSize = 3
        };

        var asyncUnaryCall = GrpcTestHelpers.CreateAsyncUnaryCall(compressionReply);

        compressionMock
            .Setup(c => c.CompressImageAsync(
                It.IsAny<CompressionRequest>(),
                It.IsAny<Metadata>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        transferMock
            .Setup(t => t.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var function = new Function(transferMock.Object, options, http, compressionMock.Object, loggerMock.Object);

        var message = new SceneAnalysisResult
        {
            WorkflowTags = [],
            Items = ["forest"]
        };
        var sqsEvent = CreateSqsEvent(JsonSerializer.Serialize(message));

        // Act
        await function.Handler(sqsEvent, Mock.Of<Amazon.Lambda.Core.ILambdaContext>());

        // Assert
        transferMock.Verify(t => t.UploadAsync(It.IsAny<TransferUtilityUploadRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        compressionMock.Verify(
            c => c.CompressImageAsync(
                It.IsAny<CompressionRequest>(),
                It.IsAny<Metadata>(),
                null,
                It.IsAny<CancellationToken>()), Times.Exactly(2));

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingMessage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.SearchingImages));
        loggerMock.VerifyLog(LogLevel.Information, Times.Exactly(2), nameof(Log.DownloadingImage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Exactly(2), nameof(Log.UploadedToBucket));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessedSuccessfully));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Never(), nameof(Log.NoImagesFound));
    }

    private static SQSEvent CreateSqsEvent(string body) => new()
    {
        Records =
        [
            new SQSEvent.SQSMessage
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Body = body
            }
        ]
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}