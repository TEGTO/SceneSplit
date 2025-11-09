using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.Api.Queries.GetObjectImages;
using SceneSplit.Configuration;
using SceneSplit.TestShared;

namespace SceneSplit.Api.UnitTests.Queries.GetObjectImages;

[TestFixture]
public class GetObjectImagesQueryHandlerTests
{
    private Mock<IAmazonS3> s3Mock;
    private Mock<IConfiguration> configMock;
    private Mock<ILogger<GetObjectImagesQueryHandler>> loggerMock;

    private GetObjectImagesQueryHandler handler;

    private const string BucketName = "test-bucket";
    private const string UserId = "user123";

    [SetUp]
    public void Setup()
    {
        s3Mock = new Mock<IAmazonS3>();
        configMock = new Mock<IConfiguration>();

        configMock.Setup(c => c[ApiConfigurationKeys.OBJECT_IMAGE_BUCKET])
            .Returns(BucketName);

        loggerMock = TestHelper.CreateLoggerMock<GetObjectImagesQueryHandler>();

        handler = new GetObjectImagesQueryHandler(
            s3Mock.Object,
            configMock.Object,
            loggerMock.Object);
    }

    private static S3Object Obj(string key, DateTime? lastModified = null)
    {
        return new()
        {
            Key = key,
            LastModified = lastModified ?? DateTime.UtcNow
        };
    }

    private static GetObjectTaggingResponse Tags(params Tag[] tags)
    {
        return new GetObjectTaggingResponse { Tagging = [.. tags] };
    }

    [Test]
    public async Task Handle_NoImagesFound_ReturnsEmptyList()
    {
        // Arrange
        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [],
                IsTruncated = false
            });

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.FetchingUserImages));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.NoImagesFoundForUser));
    }

    [Test]
    public async Task Handle_OneWorkflow_ReturnsImages()
    {
        // Arrange
        var key1 = "img1.jpg";
        var key2 = "img2.jpg";

        s3Mock.SetupSequence(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key1), Obj(key2)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == key1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == key2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ));

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result.All(r => r.ImageUrl.Contains(BucketName)));

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.LatestWorkflowResolved));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ReturningImagesForWorkflow));
    }

    [Test]
    public async Task Handle_MultipleWorkflows_ReturnsLatestWorkflowImages()
    {
        // Arrange
        var wfOld = "wf_old";
        var wfNew = "wf_new";

        var oldImg = Obj("old1.jpg", DateTime.UtcNow.AddHours(-2));
        var newImg = Obj("new1.jpg", DateTime.UtcNow.AddHours(-1));

        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [oldImg, newImg],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == oldImg.Key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = wfOld }
            ));

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == newImg.Key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = wfNew }
            ));

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.First().ImageUrl, Does.Contain(newImg.Key));

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.LatestWorkflowResolved));
    }

    [Test]
    public async Task Handle_Pagination_ReturnsAllMatchingImages()
    {
        // Arrange
        var key1 = "page1_img.jpg";
        var key2 = "page2_img.jpg";

        s3Mock.SetupSequence(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key1)],
                IsTruncated = true,
                NextContinuationToken = "token123"
            })
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key2)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ));

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ReturningImagesForWorkflow));
    }

    [Test]
    public async Task Handle_NoDescriptionTag_UsesFilenameAsDescription()
    {
        // Arrange
        var key = "photo123.png";

        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId },
                new Tag { Key = WorkflowTags.WORKFLOW_ID, Value = "wf1" }
            ));

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.First().Description, Is.EqualTo("photo123"));
    }

    [Test]
    public async Task Handle_TaggingCollectionIsNull_Continues()
    {
        // Arrange
        var key = "photo123.png";

        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectTaggingResponse());

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }


    [Test]
    public async Task Handle_UserIdTagNull_SkipsItem()
    {
        // Arrange
        var key = "photo123.png";

        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags());

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task Handle_UserIdTagDifferent_SkipsItem()
    {
        // Arrange
        var key = "photo123.png";

        s3Mock.Setup(s => s.ListObjectsV2Async(
                It.IsAny<ListObjectsV2Request>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(It.IsAny<GetObjectTaggingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(
                new Tag { Key = WorkflowTags.USER_ID_TAG, Value = "ABC" }
            ));

        // Act
        var result = await handler.Handle(new GetObjectImagesQuery(UserId), CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }
}