using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Api.HostedServices;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Queries.GetObjectImages;
using SceneSplit.Api.Sdk.Contracts;
using SceneSplit.Configuration;
using SceneSplit.TestShared;
using SceneSplit.TestShared.Extenstions;
using System.Collections.Concurrent;
using System.Reflection;

namespace SceneSplit.Api.UnitTests.HostedServices;

[TestFixture]
public class S3ObjectImageWatcherTests
{
    private const string BucketName = "test-bucket";
    private const string UserId = "user-123";

    private Mock<IAmazonS3> s3Mock;
    private Mock<IConfiguration> configMock;
    private Mock<ILogger<S3ObjectImageWatcher>> loggerMock;
    private Mock<IHubContext<SceneSplitHub, ISceneSplitHubClient>> hubContextMock;
    private Mock<IHubClients<ISceneSplitHubClient>> hubClientsMock;
    private Mock<ISceneSplitHubClient> groupClientMock;
    private Mock<IMediator> mediatorMock;

    private S3ObjectImageWatcher watcher;

    [SetUp]
    public void SetUp()
    {
        s3Mock = new Mock<IAmazonS3>();
        configMock = new Mock<IConfiguration>();
        loggerMock = TestHelper.CreateLoggerMock<S3ObjectImageWatcher>();
        hubContextMock = new Mock<IHubContext<SceneSplitHub, ISceneSplitHubClient>>();
        hubClientsMock = new Mock<IHubClients<ISceneSplitHubClient>>();
        groupClientMock = new Mock<ISceneSplitHubClient>();
        mediatorMock = new Mock<IMediator>();

        configMock.Setup(c => c[ApiConfigurationKeys.OBJECT_IMAGE_BUCKET]).Returns(BucketName);
        configMock.Setup(c => c[ApiConfigurationKeys.OBJECT_IMAGE_POLL_INTERVAL_SECONDS]).Returns("0");

        hubClientsMock
            .Setup(c => c.Group(It.Is<string>(g => g == UserId)))
            .Returns(groupClientMock.Object);

        hubContextMock
            .SetupGet(h => h.Clients)
            .Returns(hubClientsMock.Object);

        watcher = new S3ObjectImageWatcher(
            s3Mock.Object,
            configMock.Object,
            loggerMock.Object,
            hubContextMock.Object,
            mediatorMock.Object);

        var connectionToUser = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        connectionToUser!.Clear();
        connectionToUser["conn-1"] = UserId;
    }

    [TearDown]
    public void TearDown()
    {
        var connectionToUser = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        var userImages = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        connectionToUser!.Clear();
        userImages!.Clear();

        watcher.Dispose();
    }

    private static GetObjectTaggingResponse Tags(params Tag[] tags) => new() { Tagging = [.. tags] };
    private static S3Object Obj(string key) => new() { Key = key, LastModified = DateTime.UtcNow };

    private Task InvokeExecuteAsync(CancellationToken ct)
    {
        var method = typeof(S3ObjectImageWatcher).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ExecuteAsync not found");
        return (Task)method.Invoke(watcher, [ct])!;
    }

    [Test]
    public async Task ExecuteAsync_DetectsUserAndTriggersRefresh()
    {
        // Arrange
        var key = "img1.jpg";

        s3Mock.SetupSequence(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            })
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId }));

        mediatorMock.Setup(m => m.Send(
                It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ObjectImage> { new() { ImageUrl = "https://bucket/img1.jpg", Description = "img1" } });

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await cts.CancelAsync();
        });

        // Act
        await InvokeExecuteAsync(cts.Token);
        await Task.Delay(200);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStarting));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherDetectedUsers));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherRefreshStarted));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherPushedUser));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStopped));

        groupClientMock.Verify(c => c.ReceiveImageLinks(
            It.Is<ICollection<ObjectImageResponse>>(x => x.Count == 1)), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_PollingException_LogsErrorAndContinuesUntilCanceled()
    {
        // Arrange
        s3Mock.SetupSequence(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [],
                IsTruncated = false
            });

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            await cts.CancelAsync();
        });

        // Act
        await InvokeExecuteAsync(cts.Token);
        await Task.Delay(200);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherStarting));
        loggerMock.VerifyLog(LogLevel.Error, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherPollingError));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherStopped));
    }

    [Test]
    public async Task ExecuteAsync_CancellationDuringDelay_EmitsCancellationAndStopped()
    {
        // Arrange
        s3Mock.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response { S3Objects = [], IsTruncated = false });

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            await cts.CancelAsync();
            await Task.Delay(100);
        });

        // Act
        await InvokeExecuteAsync(cts.Token);
        await Task.Delay(400);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStarting));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherCancellation));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStopped));
    }

    [Test]
    public async Task ExecuteAsync_CancellationDuringScan_EmitsCancellationAndStopped()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        s3Mock.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .Callback(cts.Cancel)
            .ThrowsAsync(new TaskCanceledException());

        // Act
        await InvokeExecuteAsync(cts.Token);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStarting));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherCancellation));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStopped));
    }

    [Test]
    public async Task ExecuteAsync_NoNewUsers_DoesNotEmitDetectedUsersLog()
    {
        // Arrange
        s3Mock.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [],
                IsTruncated = false
            });

        using var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            await Task.Delay(30);
            await cts.CancelAsync();
        });

        // Act
        await InvokeExecuteAsync(cts.Token);

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStarting));
        loggerMock.VerifyLog(LogLevel.Information, Times.Never(), nameof(Log.S3ObjectImageWatcherDetectedUsers));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherStopped));
    }

    [Test]
    public async Task ScanForNewUserIds_ReturnsUserId_WhenTaggedObjectAppears()
    {
        // Arrange
        var key = "img1.jpg";
        s3Mock.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId }));

        var scanMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("ScanForNewUserIds", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ScanForNewUserIds not found");

        // Act + Assert
        var result = await (Task<HashSet<string>>)scanMethod.Invoke(watcher, [CancellationToken.None])!;
        Assert.That(result, Does.Contain(UserId));
    }

    [Test]
    public async Task ScanForNewUserIds_SkipsKnownKeys_DoesNotTagTwice()
    {
        // Arrange
        var key = "dup.jpg";

        s3Mock.Setup(s => s.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListObjectsV2Response
            {
                S3Objects = [Obj(key)],
                IsTruncated = false
            });

        s3Mock.Setup(s => s.GetObjectTaggingAsync(
                It.Is<GetObjectTaggingRequest>(r => r.Key == key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tags(new Tag { Key = WorkflowTags.USER_ID_TAG, Value = UserId }));

        // Act + Assert
        var scanMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("ScanForNewUserIds", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var first = await (Task<HashSet<string>>)scanMethod.Invoke(watcher, [CancellationToken.None])!;
        Assert.That(first, Does.Contain(UserId));

        var second = await (Task<HashSet<string>>)scanMethod.Invoke(watcher, [CancellationToken.None])!;
        Assert.That(second, Is.Empty);

        s3Mock.Verify(s => s.GetObjectTaggingAsync(
            It.Is<GetObjectTaggingRequest>(r => r.Key == key),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task TriggerUserRefresh_InvokesMediatorAndPushesImages()
    {
        // Arrange
        var images = new List<ObjectImage> { new() { ImageUrl = "https://bucket/img1.jpg", Description = "img1" } };

        mediatorMock.Setup(m => m.Send(
                It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(images);

        var triggerMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("TriggerUserRefresh", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("TriggerUserRefresh not found");

        // Act
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);
        await WaitForUserRefreshTasksToComplete(UserId, TimeSpan.FromSeconds(2));

        // Assert
        mediatorMock.Verify(m => m.Send(
            It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);

        groupClientMock.Verify(c => c.ReceiveImageLinks(
            It.Is<ICollection<ObjectImageResponse>>(x => x.Count == 1)), Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherRefreshStarted));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.S3ObjectImageWatcherPushedUser));
    }

    [Test]
    public async Task TriggerUserRefresh_CancelsPreviousRefresh_WhenCalledAgainQuickly()
    {
        // Arrange
        mediatorMock.SetupSequence(m => m.Send(
            It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
            It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(200), new CancellationTokenSource(50).Token);
                }
                catch (OperationCanceledException)
                {
                    // Swallow for test
                }
                return [new() { ImageUrl = "first", Description = "first" }];
            })
            .ReturnsAsync([new() { ImageUrl = "second", Description = "second" }]);

        var triggerMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("TriggerUserRefresh", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);
        await Task.Delay(25);
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);

        await WaitForUserRefreshTasksToComplete(UserId, TimeSpan.FromSeconds(2));

        // Assert
        mediatorMock.Verify(m => m.Send(
            It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        groupClientMock.Verify(c => c.ReceiveImageLinks(
            It.Is<ICollection<ObjectImageResponse>>(x => x.Any(r => r.ImageUrl == "second"))),
            Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeast(2), nameof(Log.S3ObjectImageWatcherRefreshStarted));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherRefreshCanceled));
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherPushedUser));
    }

    [Test]
    public async Task TriggerUserRefresh_WhenMediatorCancelled_LogsCancellationInTask()
    {
        // Arrange
        mediatorMock.Setup(m => m.Send(
                It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
                It.IsAny<CancellationToken>()))
            .Returns(async (GetObjectImagesQuery _, CancellationToken t) =>
            {
                await Task.Delay(Timeout.Infinite, t);
                return [];
            });

        var triggerMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("TriggerUserRefresh", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);
        await Task.Delay(10);
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);

        await WaitForUserRefreshTasksToComplete(UserId, TimeSpan.FromSeconds(2));

        // Assert
        loggerMock.VerifyLog(LogLevel.Information, Times.AtLeastOnce(), nameof(Log.S3ObjectImageWatcherCancellation));
    }

    [Test]
    public async Task TriggerUserRefresh_WhenUserNotConnected_DoesNotPushImages()
    {
        // Arrange
        var connectionToUser = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        connectionToUser!.Clear();

        mediatorMock.Setup(m => m.Send(
                It.IsAny<GetObjectImagesQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var triggerMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("TriggerUserRefresh", BindingFlags.Instance | BindingFlags.NonPublic)!;

        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);
        await Task.Delay(100);

        // Assert
        groupClientMock.Verify(c => c.ReceiveImageLinks(It.IsAny<ICollection<ObjectImageResponse>>()), Times.Never);
    }

    private async Task WaitForUserRefreshTasksToComplete(string userId, TimeSpan timeout)
    {
        var end = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < end)
        {
            if (typeof(S3ObjectImageWatcher)
                .GetField("userRefreshTasks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(watcher) is not ConcurrentDictionary<string, Task> tasksDict ||
                !tasksDict.TryGetValue(userId, out var task))
            {
                return;
            }

            if (task.IsCompleted)
            {
                return;
            }

            await Task.Delay(250);
        }

        Assert.Fail("User refresh task did not complete within timeout.");
    }

    [Test]
    public async Task TriggerUserRefresh_WhenMediatorThrows_LogsRefreshError()
    {
        // Arrange
        mediatorMock.Setup(m => m.Send(
                It.Is<GetObjectImagesQuery>(q => q.UserId == UserId),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("refresh failed"));

        var triggerMethod = typeof(S3ObjectImageWatcher)
            .GetMethod("TriggerUserRefresh", BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        triggerMethod.Invoke(watcher, [UserId, CancellationToken.None]);
        await WaitForUserRefreshTasksToComplete(UserId, TimeSpan.FromSeconds(2));

        // Assert
        loggerMock.VerifyLog(LogLevel.Error, Times.Once(), nameof(Log.S3ObjectImageWatcherRefreshError));
        groupClientMock.Verify(c => c.ReceiveImageLinks(It.IsAny<ICollection<ObjectImageResponse>>()), Times.Never);
    }
}