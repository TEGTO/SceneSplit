using Google.Protobuf;
using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SceneSplit.Api.Commands.ProcessSceneImage;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Sdk;
using SceneSplit.TestShared;
using SceneSplit.TestShared.Helpers;
using System.Text.RegularExpressions;

namespace SceneSplit.Api.UnitTests.Commands.ProcessSceneImage;

[TestFixture]
public partial class ProcessSceneImageCommandHandlerTests
{
    private Mock<Compression.CompressionClient> compressionClientMock;
    private Mock<IStorageService> storageServiceMock;
    private Mock<IConfiguration> configMock;
    private Mock<ILogger<ProcessSceneImageCommandHandler>> loggerMock;

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

        loggerMock = TestHelper.CreateLoggerMock<ProcessSceneImageCommandHandler>();

        handler = new ProcessSceneImageCommandHandler(
            compressionClientMock.Object,
            storageServiceMock.Object,
            configMock.Object,
            loggerMock.Object);
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
        var command = new ProcessSceneImageCommand("user123", "scene.png", fakeData);

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
                    GuidRegex().IsMatch(Path.GetFileNameWithoutExtension(r.FileName)) &&
                    r.FileName.Contains(".png") &&
                    r.Quality == 75),
                null,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);

        storageServiceMock.Verify(s =>
            s.UploadSceneImageAsync(
                It.Is<string>(filename =>
                    GuidRegex().IsMatch(Path.GetFileNameWithoutExtension(filename)) &&
                    Path.GetExtension(filename).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                ),
                It.IsAny<byte[]>(),
                "image/jpg",
                command.UserId,
                It.IsAny<CancellationToken>()),
            Times.Once);

        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ProcessingSceneImage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.SendingCompressionRequest));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.ImageCompressed));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.UploadingToStorage));
        loggerMock.VerifyLog(LogLevel.Information, Times.Once(), nameof(Log.UploadCompleted));
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
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.UnsupportedExtension));
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
        loggerMock.VerifyLog(LogLevel.Warning, Times.Once(), nameof(Log.FileTooLarge));
    }

    [GeneratedRegex(@"^[0-9a-fA-F\-]{36}$")]
    private static partial Regex GuidRegex();
}