using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.Configuration;
using SceneSplit.TestShared;

namespace SceneSplit.ObjectImageCleanerLambda.UnitTests;

[TestFixture]
public class FunctionTests
{
    private const string Bucket = "bucket-a";
    private const string TriggerKey = "wf2/img-new.jpg";
    private const string UserId = "user-123";
    private const string KeepWorkflow = "wf2";

    private Mock<IAmazonS3> s3Mock;
    private Mock<ILogger<Function>> loggerMock;
    private ObjectImageCleanerLambdaOptions options;

    private Function function;

    [SetUp]
    public void SetUp()
    {
        s3Mock = new Mock<IAmazonS3>();
        loggerMock = TestHelper.CreateLoggerMock<Function>();
        options = new ObjectImageCleanerLambdaOptions
        {
            MaxKeysSearchConcurrency = 8,
            MaxDeleteBatch = 1000
        };

        function = new Function(s3Mock.Object, loggerMock.Object, options);
    }

    private static S3Event CreateEvent((string Bucket, string Key) s3Object)
    {
        var s3Entity = new S3Event.S3Entity
        {
            Bucket = new S3Event.S3BucketEntity { Name = s3Object.Bucket },
            Object = new S3Event.S3ObjectEntity { Key = s3Object.Key }
        };

        var record = new S3Event.S3EventNotificationRecord
        {
            S3 = s3Entity
        };

        return new S3Event
        {
            Records = [record]
        };
    }

    [Test]
    public async Task Handler_WhenMissingRequiredTags_SkipsCleanup()
    {
        // Arrange
        var s3Event = CreateEvent((Bucket, TriggerKey));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.BucketName == Bucket && r.Key == TriggerKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse { Tagging = [] });

        // Act
        await function.Handler(s3Event);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.CleanerTriggered));
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.SkippingCleanupMissingTags));

        s3Mock.Verify(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()), Times.Never);
        s3Mock.Verify(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handler_DeletesObjectsForSameUser_WithDifferentOrMissingWorkflow()
    {
        // Arrange
        var keysOnBucket = new[]
        {
            TriggerKey,
            "wf1/old-1.jpg",
            "wf2/keep-1.jpg",
            "no-wf/missing.jpg",
            "other-user/ignore.jpg"
        };

        var s3Event = CreateEvent((Bucket, TriggerKey));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == TriggerKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = KeepWorkflow }
            ]
        });

        s3Mock.Setup(s => s.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.BucketName == Bucket),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ListObjectsV2Response
        {
            S3Objects = keysOnBucket.Select(k => new S3Object { Key = k }).ToList(),
            IsTruncated = false
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == "wf1/old-1.jpg"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ]
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == "wf2/keep-1.jpg"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = KeepWorkflow }
            ]
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == "no-wf/missing.jpg"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId }
            ]
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == "other-user/ignore.jpg"),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = "someone-else" },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wfZ" }
            ]
        });

        DeleteObjectsRequest? capturedDelete = null;
        s3Mock.Setup(s => s.DeleteObjectsAsync(
                It.IsAny<DeleteObjectsRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectsRequest, CancellationToken>((req, _) => capturedDelete = req)
            .ReturnsAsync(new DeleteObjectsResponse());

        // Act
        await function.Handler(s3Event);

        // Assert
        Assert.That(capturedDelete, Is.Not.Null);
        var deletedKeys = capturedDelete!.Objects.Select(o => o.Key).ToHashSet();
        Assert.That(deletedKeys, Is.EquivalentTo(["wf1/old-1.jpg", "no-wf/missing.jpg"]));

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.StartingCleanup));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.DeletedBatch));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.CleanupCompleted));
    }

    [Test]
    public async Task Handler_WhenTagReadFails_LogsWarningAndContinues()
    {
        // Arrange
        var otherKey = "wf0/error.jpg";
        var keepKey = "wf2/keep-ok.jpg";

        var s3Event = CreateEvent((Bucket, TriggerKey));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == TriggerKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = KeepWorkflow }
            ]
        });

        s3Mock.Setup(s => s.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.BucketName == Bucket),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ListObjectsV2Response
        {
            S3Objects =
            [
                new() { Key = TriggerKey },
                new() { Key = otherKey },
                new() { Key = keepKey }
            ],
            IsTruncated = false
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == otherKey),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("boom"));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == keepKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = KeepWorkflow }
            ]
        });

        s3Mock.Setup(s => s.DeleteObjectsAsync(
                It.IsAny<DeleteObjectsRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeleteObjectsResponse());

        // Act
        await function.Handler(s3Event);

        // Assert
        loggerMock.VerifyLog(LogLevel.Warning, Times.AtLeastOnce(), nameof(Log.FailedToReadTags));
        s3Mock.Verify(s => s.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.CleanupCompleted));
    }

    [Test]
    public async Task Handler_LogsDeleteErrors_WhenS3ReturnsErrors()
    {
        // Arrange
        var oldKey = "wf1/old.jpg";
        var s3Event = CreateEvent((Bucket, TriggerKey));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == TriggerKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = KeepWorkflow }
            ]
        });

        s3Mock.Setup(s => s.ListObjectsV2Async(
            It.Is<ListObjectsV2Request>(r => r.BucketName == Bucket),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new ListObjectsV2Response
        {
            S3Objects =
            [
                new() { Key = TriggerKey },
                new() { Key = oldKey }
            ],
            IsTruncated = false
        });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == oldKey),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new GetObjectTaggingResponse
        {
            Tagging =
            [
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ]
        });

        s3Mock.Setup(s => s.DeleteObjectsAsync(
            It.IsAny<DeleteObjectsRequest>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new DeleteObjectsResponse
        {
            DeleteErrors =
            [
                new() { Key = oldKey, Code = "AccessDenied", Message = "denied" }
            ]
        });

        // Act
        await function.Handler(s3Event);

        // Assert
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.DeleteError));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.CleanupCompleted));
    }
}