using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using SceneSplit.Api.Commands.ProcessSceneImage;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Sdk;
using SceneSplit.TestShared.Helpers;

namespace SceneSplit.Api.UnitTests.Commands.ProcessSceneImage;

[TestFixture]
public class ProcessSceneImageCommandHandlerTests
{
    private Mock<Compression.CompressionClient> compressionClientMock;
    private Mock<IStorageService> storageServiceMock;
    private Mock<IConfiguration> configMock;

    private ProcessSceneImageCommandHandler handler;

    [SetUp]
    public void Setup()
    {
        compressionClientMock = new Mock<Compression.CompressionClient>();
        storageServiceMock = new Mock<IStorageService>();
        configMock = new Mock<IConfiguration>();

        configMock.Setup(c => c[It.Is<string>(s => s == ApiConfigurationKeys.ALLOWED_IMAGE_TYPES)])
            .Returns(".jpg,.jpeg,.png");

        configMock.Setup(c => c[It.Is<string>(s => s == ApiConfigurationKeys.MAX_IMAGE_SIZE)])
            .Returns((1024 * 1024 * 5).ToString());

        handler = new ProcessSceneImageCommandHandler(
            compressionClientMock.Object,
            storageServiceMock.Object,
            configMock.Object);
    }

    private static byte[] CreateFakeImageData(int sizeInKb = 50)
    {
        var data = new byte[sizeInKb * 1024];
        new Random().NextBytes(data);
        return data;
    }

    [Test]
    public async Task Handle_ValidRequest_CompressesAndUploadsImage()
    {
        // Arrange
        var fakeData = CreateFakeImageData();
        var command = new ProcessSceneImageCommand("user123", "scene.jpg", fakeData);

        var compressionReply = new CompressionReply
        {
            CompressedImage = ByteString.CopyFrom(new byte[1024]),
            Format = "jpg",
            OriginalSize = fakeData.Length,
            CompressedSize = 1024
        };

        var asyncUnaryCall = GrpcTestHelpers.CreateAsyncUnaryCall(compressionReply);

        compressionClientMock
            .Setup(c => c.CompressImageAsync(
                It.IsAny<CompressionRequest>(),
                It.IsAny<Metadata>(),
                null,
                It.IsAny<CancellationToken>()))
            .Returns(asyncUnaryCall);

        storageServiceMock
            .Setup(s => s.UploadSceneImageAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        compressionClientMock.Verify(c =>
            c.CompressImageAsync(
                It.Is<CompressionRequest>(r =>
                    r.FileName == command.FileName &&
                    r.Quality == 75),
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        storageServiceMock.Verify(s =>
            s.UploadSceneImageAsync(
                command.FileName,
                It.IsAny<byte[]>(),
                "image/jpg",
                command.UserId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void Handle_UnsupportedExtension_ThrowsHubException()
    {
        // Arrange
        var fakeData = CreateFakeImageData();
        var command = new ProcessSceneImageCommand("user123", "scene.bmp", fakeData);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.That(ex.Message, Does.Contain("not supported"));
    }

    [Test]
    public void Handle_FileTooLarge_ThrowsHubException()
    {
        // Arrange
        var bigData = new byte[1024 * 1024 * 10];
        var command = new ProcessSceneImageCommand("user123", "scene.jpg", bigData);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(() =>
            handler.Handle(command, CancellationToken.None));

        Assert.That(ex.Message, Does.Contain("File size exceeds"));
    }
}