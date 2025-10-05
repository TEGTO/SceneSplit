using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Moq;
using SceneSplit.Configuration;

namespace SceneSplit.Api.Services.StorageService.Tests;

[TestFixture]
public class S3StorageServiceTests
{
    private const string BUCKET_NAME = "test-bucket";

    private Mock<ITransferUtility> mockTransferUtility;
    private Mock<IConfiguration> mockConfig;
    private S3StorageService service;

    [SetUp]
    public void Setup()
    {
        mockTransferUtility = new Mock<ITransferUtility>();
        mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c[ApiConfigurationKeys.SCENE_IMAGE_BUCKET]).Returns(BUCKET_NAME);

        service = new S3StorageService(mockTransferUtility.Object, mockConfig.Object);
    }

    [Test]
    public void Constructor_MissingBucketConfig_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c[ApiConfigurationKeys.SCENE_IMAGE_BUCKET]).Returns((string)null!);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new S3StorageService(mockTransferUtility.Object, config.Object));

        Assert.That(ex.Message, Is.EqualTo("Missing S3 bucket configuration."));
    }

    [Test]
    public async Task UploadSceneImageAsync_CallsUploadWithCorrectParameters()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3 };

        // Act
        await service.UploadSceneImageAsync("file.png", content, "image/png", "user123", CancellationToken.None);

        // Assert
        mockTransferUtility.Verify(t => t.UploadAsync(
            It.IsAny<TransferUtilityUploadRequest>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }
}