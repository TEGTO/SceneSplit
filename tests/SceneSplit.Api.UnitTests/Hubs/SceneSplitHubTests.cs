using MediatR;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SceneSplit.Api.Commands.ProcessSceneImage;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Api.Hubs;
using SceneSplit.Api.Queries.GetObjectImages;
using SceneSplit.Api.Sdk.Contracts;
using SceneSplit.TestShared.Extenstions;
using System.Collections.Concurrent;

namespace SceneSplit.Api.UnitTests.Hubs;

[TestFixture]
public class SceneSplitHubTests
{
    private Mock<IMediator> mockMediator;
    private Mock<HubCallerContext> mockContext;
    private Mock<IGroupManager> mockGroups;
    private Mock<ISceneSplitHubClient> mockClients;
    private Mock<IHubCallerClients<ISceneSplitHubClient>> mockHubCallerClients;

    private SceneSplitHub sceneSplitHub;

    [SetUp]
    public void Setup()
    {
        mockMediator = new Mock<IMediator>();
        mockContext = new Mock<HubCallerContext>();
        mockGroups = new Mock<IGroupManager>();
        mockClients = new Mock<ISceneSplitHubClient>();

        mockHubCallerClients = new Mock<IHubCallerClients<ISceneSplitHubClient>>();
        mockHubCallerClients.Setup(c => c.Caller).Returns(mockClients.Object);

        sceneSplitHub = new SceneSplitHub(mockMediator.Object)
        {
            Context = mockContext.Object,
            Groups = mockGroups.Object,
            Clients = mockHubCallerClients.Object
        };
    }

    [TearDown]
    public void TearDown()
    {
        sceneSplitHub.Dispose();
    }

    [Test]
    public async Task StartListenToUserObjectImages_AddsClientToGroupAndReturnsImages()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "connection1";
        var objectImages = new List<ObjectImage> { new() { ImageUrl = "some-url", Description = "some-img" } };

        mockContext.SetupGet(c => c.ConnectionId).Returns(connectionId);
        mockMediator.Setup(m => m.Send(It.IsAny<GetObjectImagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(objectImages);

        // Act
        await sceneSplitHub.StartListenToUserObjectImages(userId);

        // Assert
        mockGroups.Verify(g => g.AddToGroupAsync(connectionId, userId, default), Times.Once);
        mockClients.Verify(c => c.ReceiveImageLinks(It.Is<ICollection<ObjectImageResponse>>(imgs => imgs.Count == 1)), Times.Once);

        var userObjectImages = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");
        Assert.That(userObjectImages, Is.Not.Null);
        Assert.That(userObjectImages.ContainsKey(userId), Is.True);
        Assert.That(userObjectImages[userId].First().ImageUrl, Is.EqualTo(objectImages[0].ImageUrl));
    }

    [Test]
    public async Task OnDisconnectedAsync_RemovesClientFromGroupAndUserImagesIfLastConnection()
    {
        // Arrange
        var userId = "user2";
        var connectionId = "connection2";
        var objectImages = new List<ObjectImage> { new() { ImageUrl = "some-url", Description = "some-img" } };

        var connectionToUser = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        var userObjectImages = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        Assert.That(connectionToUser, Is.Not.Null);
        Assert.That(userObjectImages, Is.Not.Null);

        connectionToUser[connectionId] = userId;
        userObjectImages[userId] = objectImages;

        mockContext.SetupGet(c => c.ConnectionId).Returns(connectionId);

        // Act
        await sceneSplitHub.OnDisconnectedAsync(null);

        // Assert
        mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId, userId, default), Times.Once);

        connectionToUser = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        userObjectImages = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        Assert.That(connectionToUser, Is.Not.Null);
        Assert.That(userObjectImages, Is.Not.Null);

        Assert.That(connectionToUser.ContainsKey(connectionId), Is.False);
        Assert.That(userObjectImages.ContainsKey(userId), Is.False);
    }

    [Test]
    public async Task UploadSceneImageForUser_CallsSceneImageProcessor()
    {
        // Arrange
        var userId = "user1";
        var request = new SendSceneImageRequest { FileName = "scene.png", FileContent = [1, 2, 3] };

        // Act
        await sceneSplitHub.UploadSceneImageForUser(userId, request);

        // Assert
        mockMediator.Verify(m => m.Send(It.IsAny<ProcessSceneImageCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnDisconnectedAsync_DoesNotRemoveImagesIfOtherConnectionsExist()
    {
        // Arrange
        var userId = "user1";
        var connectionId1 = "connection1";
        var connectionId2 = "connection2";
        var images = new List<ObjectImage> { new() { ImageUrl = "some-url", Description = "some-img" } };

        var connectionToUser = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        var userObjectImages = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        Assert.That(connectionToUser, Is.Not.Null);
        Assert.That(userObjectImages, Is.Not.Null);

        connectionToUser[connectionId1] = userId;
        connectionToUser[connectionId2] = userId;
        userObjectImages[userId] = images;

        mockContext.SetupGet(c => c.ConnectionId).Returns(connectionId1);

        // Act
        await sceneSplitHub.OnDisconnectedAsync(null);

        // Assert
        mockGroups.Verify(g => g.RemoveFromGroupAsync(connectionId1, userId, default), Times.Once);
        Assert.That(userObjectImages.ContainsKey(userId), Is.True);
    }

    [Test]
    public async Task StartListenToUserObjectImages_WhenCalledTwice_UsesCachedImagesAndDoesNotCallMediatorAgain()
    {
        // Arrange
        var userId = "user-cache-test";
        var connectionId1 = "conn1";
        var connectionId2 = "conn2";

        var objectImages = new List<ObjectImage>
        {
            new() { ImageUrl = "cached-url", Description = "img1" }
        };

        mockMediator.Setup(m => m.Send(It.IsAny<GetObjectImagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(objectImages);

        mockContext.SetupGet(c => c.ConnectionId).Returns(connectionId1);

        // Act
        await sceneSplitHub.StartListenToUserObjectImages(userId);

        // Assert
        mockMediator.Verify(m => m.Send(It.IsAny<GetObjectImagesQuery>(), It.IsAny<CancellationToken>()), Times.Once);

        // Act
        mockContext.SetupGet(c => c.ConnectionId).Returns(connectionId2);
        await sceneSplitHub.StartListenToUserObjectImages(userId);

        // Assert
        mockMediator.Verify(m => m.Send(It.IsAny<GetObjectImagesQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        mockGroups.Verify(g => g.AddToGroupAsync(connectionId2, userId, default), Times.Once);
    }

    [Test]
    public async Task OnDisconnectedAsync_WhenConnectionIdNotTracked_ReturnsWithoutRemovingGroup()
    {
        // Arrange
        var nonExistingConnection = "conn-not-tracked";
        mockContext.SetupGet(c => c.ConnectionId).Returns(nonExistingConnection);

        var connectionToUser = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");
        var userObjectImages = typeof(SceneSplitHub)
            .GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        connectionToUser?.Clear();
        userObjectImages?.Clear();

        // Act
        await sceneSplitHub.OnDisconnectedAsync(null);

        // Assert
        mockGroups.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);

        Assert.That(connectionToUser?.Count, Is.EqualTo(0));
        Assert.That(userObjectImages?.Count, Is.EqualTo(0));
    }
}