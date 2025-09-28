using Microsoft.AspNetCore.SignalR;
using Moq;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Api.Sdk.Contracts;
using SceneSplit.Api.Sevices.ImagePersistent;
using SceneSplit.Api.Sevices.SceneImageProcessor;
using SceneSplit.TestShared.Extenstions;
using System.Collections.Concurrent;

namespace SceneSplit.Api.Hubs.Tests;

internal class SceneSplitHubTests
{
    private Mock<IImagePersistentService> mockImagePersistent;
    private Mock<ISceneImageProcessor> mockSceneImageProcessor;
    private Mock<HubCallerContext> mockContext;
    private Mock<IGroupManager> mockGroups;
    private Mock<ISceneSplitHubClient> mockClients;
    private Mock<IHubCallerClients<ISceneSplitHubClient>> mockHubCallerClients;

    private SceneSplitHub sceneSplitHub;

    [SetUp]
    public void Setup()
    {
        mockImagePersistent = new Mock<IImagePersistentService>();
        mockSceneImageProcessor = new Mock<ISceneImageProcessor>();
        mockContext = new Mock<HubCallerContext>();
        mockGroups = new Mock<IGroupManager>();
        mockClients = new Mock<ISceneSplitHubClient>();

        mockHubCallerClients = new Mock<IHubCallerClients<ISceneSplitHubClient>>();
        mockHubCallerClients.Setup(c => c.Caller).Returns(mockClients.Object);

        sceneSplitHub = new SceneSplitHub(mockImagePersistent.Object, mockSceneImageProcessor.Object)
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
        mockImagePersistent.Setup(s => s.GetObjectImagesForUserAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(objectImages);

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
    public async Task UpdateImagesForUser_UpdatesImagesAndNotifiesClients()
    {
        // Arrange
        var userId = "user1";
        var connectionId = "connection1";
        var images = new List<ObjectImage> { new() { ImageUrl = "some-url", Description = "some-img" } };

        var connectionToUser = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, string>>("connectionToUser");

        mockHubCallerClients.Setup(x => x.Group(userId)).Returns(mockClients.Object);

        Assert.That(connectionToUser, Is.Not.Null);

        connectionToUser[connectionId] = userId;

        // Act
        await sceneSplitHub.UpdateImagesForUser(userId, images);

        // Assert
        mockImagePersistent.Verify(s => s.UpdateObjectImagesForUserAsync(userId, images, It.IsAny<CancellationToken>()), Times.Once);
        mockClients.Verify(c => c.ReceiveImageLinks(It.Is<ICollection<ObjectImageResponse>>(imgs => imgs.Count == 1)), Times.Once);

        var userObjectImages = typeof(SceneSplitHub).GetStaticFieldValue<ConcurrentDictionary<string, ICollection<ObjectImage>>>("userObjectImages");

        Assert.That(userObjectImages, Is.Not.Null);
        Assert.That(images, Is.EqualTo(userObjectImages[userId]));
    }

    [Test]
    public async Task UploadSceneImageForUser_CallsSceneImageProcessor()
    {
        // Arrange
        var userId = "user1";
        var request = new SendSceneImageRequest { FileName = "scene.png", FileContent = new byte[] { 1, 2, 3 } };

        // Act
        await sceneSplitHub.UploadSceneImageForUser(userId, request);

        // Assert
        mockSceneImageProcessor.Verify(p => p.ProcessSceneImageForUserAsync(
            userId, request.FileName, request.FileContent, It.IsAny<CancellationToken>()), Times.Once);
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
}