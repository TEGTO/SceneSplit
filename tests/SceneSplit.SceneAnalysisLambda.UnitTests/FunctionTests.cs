using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.TestShared;
using System.Text;
using System.Text.Json;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Tag = Amazon.S3.Model.Tag;

namespace SceneSplit.SceneAnalysisLambda.UnitTests;

[TestFixture]
public class FunctionTests
{
    private Mock<IAmazonS3> s3Mock = null!;
    private Mock<IAmazonSQS> sqsMock = null!;
    private Mock<IChatClient> aiMock = null!;
    private Mock<ILambdaContext> contextMock = null!;
    private Mock<ILogger<Function>> loggerMock = null!;

    private SceneAnalysisLambdaOptions options = null!;
    private Function function = null!;

    [SetUp]
    public void Setup()
    {
        s3Mock = new Mock<IAmazonS3>();
        sqsMock = new Mock<IAmazonSQS>();
        aiMock = new Mock<IChatClient>();

        options = new SceneAnalysisLambdaOptions
        {
            MaxItems = 5,
            SqsQueueUrl = "https://queue-url"
        };

        loggerMock = TestHelper.CreateLoggerMock<Function>();
        contextMock = new Mock<ILambdaContext>();

        function = new Function(
            s3Mock.Object,
            sqsMock.Object,
            aiMock.Object,
            options,
            loggerMock.Object
        );
    }

    [Test]
    public async Task Handler_ObjectWithTagsAndItems_SendsSqsMessage()
    {
        var bucket = "test-bucket";
        var key = "test.jpg";

        var tags = new List<Tag>
        {
            new() { Key = "UserId", Value = "123" },
            new() { Key = "ProjectId", Value = "abc" }
        };

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectTaggingResponse { Tagging = tags });

        s3Mock.Setup(s => s.GetObjectAsync(bucket, key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("image bytes"))
            });

        aiMock.Setup(ai => ai.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken ct) =>
            {
                var response = new SceneAnalysisAIResponse { Items = ["item1", "item2"] };
                var assistantMessage = new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(response));
                return new ChatResponse<SceneAnalysisAIResponse>(new ChatResponse(assistantMessage), new JsonSerializerOptions());
            });

        var s3Event = new S3Event
        {
            Records =
            [
                new()
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = bucket },
                        Object = new S3Event.S3ObjectEntity { Key = key }
                    }
                }
            ]
        };

        await function.Handler(s3Event, contextMock.Object);

        sqsMock.Verify(s => s.SendMessageAsync(
            It.Is<SendMessageRequest>(req => req.QueueUrl == options.SqsQueueUrl && req.MessageBody.Contains("item1")),
            It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingS3Object));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.PublishedAnalysisResult));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Never(), nameof(Log.NoItemsDetected));
    }

    [Test]
    public async Task Handler_ObjectWithNoTags_DefaultsToUnknown()
    {
        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectTaggingResponse { Tagging = null });

        s3Mock.Setup(s => s.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("image bytes"))
            });

        aiMock.Setup(ai => ai.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse<SceneAnalysisAIResponse>(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(new SceneAnalysisAIResponse { Items = ["item1"] }))),
                new JsonSerializerOptions()));

        var s3Event = new S3Event
        {
            Records =
            [
                new()
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = "bucket" },
                        Object = new S3Event.S3ObjectEntity { Key = "key.jpg" }
                    }
                }
            ]
        };

        await function.Handler(s3Event, contextMock.Object);

        sqsMock.Verify(s => s.SendMessageAsync(
            It.Is<SendMessageRequest>(req => req.MessageBody.Contains("\"UserId\":\"unknown\"")),
            It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingS3Object));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.PublishedAnalysisResult));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Never(), nameof(Log.NoItemsDetected));
    }

    [Test]
    public async Task Handler_NoItemsFromAI_DoesNotSendSqsMessage()
    {
        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectTaggingResponse { Tagging = [new() { Key = "UserId", Value = "123" }] });

        s3Mock.Setup(s => s.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("image bytes")) });

        aiMock.Setup(ai => ai.GetResponseAsync(
            It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse<SceneAnalysisAIResponse>(
                new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(new SceneAnalysisAIResponse { Items = [] }))),
                new JsonSerializerOptions()));

        var s3Event = new S3Event
        {
            Records =
            [
                new()
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = "bucket" },
                        Object = new S3Event.S3ObjectEntity { Key = "key.jpg" }
                    }
                }
            ]
        };

        await function.Handler(s3Event, contextMock.Object);

        sqsMock.Verify(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Never);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingS3Object));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.NoItemsDetected));
        loggerMock.VerifyLog(LogLevel.Information, Times.Never(), nameof(Log.PublishedAnalysisResult));
    }

    [Test]
    public void Handler_SqsSendMessageThrows_CaughtAndLogged()
    {
        var bucket = "bucket";
        var key = "key.jpg";

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectTaggingResponse { Tagging = [new Tag { Key = "UserId", Value = "123" }] });

        s3Mock.Setup(s => s.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse { ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes("image bytes")) });

        aiMock.Setup(ai => ai.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ChatMessage> messages, ChatOptions? _, CancellationToken __) =>
            {
                var response = new SceneAnalysisAIResponse { Items = ["item1", "item2"] };
                var assistantMessage = new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(response));
                return new ChatResponse<SceneAnalysisAIResponse>(new ChatResponse(assistantMessage), new JsonSerializerOptions());
            });

        sqsMock.Setup(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SQS failure"));

        var s3Event = new S3Event
        {
            Records =
            [
                new()
                {
                    S3 = new S3Event.S3Entity
                    {
                        Bucket = new S3Event.S3BucketEntity { Name = bucket },
                        Object = new S3Event.S3ObjectEntity { Key = key }
                    }
                }
            ]
        };

        Assert.ThrowsAsync<Exception>(async () => await function.Handler(s3Event, contextMock.Object));

        sqsMock.Verify(s => s.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingS3Object));
        loggerMock.VerifyLog(LogLevel.Error, Times.Once(), nameof(Log.FailedToProcessImage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Never(), nameof(Log.PublishedAnalysisResult));
    }
}