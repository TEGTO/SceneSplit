using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Moq;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Api.Services;
using SceneSplit.ImageCompression.Sdk;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace SceneSplit.ImageCompression.Api.UnitTests.Services;

[TestFixture]
public class CompressionServiceTests
{
    private CompressionService service;
    private Mock<IConfiguration> configMock;

    private Mock<ServerCallContext> contextMock;

    [SetUp]
    public void Setup()
    {
        configMock = new Mock<IConfiguration>();

        configMock.Setup(c => c[It.Is<string>(k => k == ImageCompressionApiConfigurationKeys.ALLOWED_IMAGE_TYPES)])
            .Returns(".jpg,.jpeg,.png");

        configMock.Setup(c => c[It.Is<string>(k => k == ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE)])
            .Returns((1024 * 1024 * 5).ToString());

        service = new CompressionService(configMock.Object);
        contextMock = new Mock<ServerCallContext>();
    }

    private static byte[] CreateTestImage(int width = 100, int height = 100)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.Save(ms, new JpegEncoder { Quality = 90 });
        return ms.ToArray();
    }

    [Test]
    public async Task CompressImage_ValidJpegImage_ReturnsCompressedImage()
    {
        // Arrange
        var imageData = CreateTestImage();
        var request = new CompressionRequest
        {
            FileName = "test.png",
            ImageData = ByteString.CopyFrom(imageData),
            Quality = 50
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.CompressedImage.Length, Is.LessThan(request.ImageData.Length));
            Assert.That(result.Format, Is.EqualTo("png"));
        }
    }

    [Test]
    public async Task CompressImage_ValidPngImage_ReturnsCompressedImage()
    {
        // Arrange
        using var image = new Image<Rgba32>(50, 50);
        using var ms = new MemoryStream();
        await image.SaveAsync(ms, new PngEncoder());
        var request = new CompressionRequest
        {
            FileName = "test.png",
            ImageData = ByteString.CopyFrom(ms.ToArray()),
            Quality = 60
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        using (Assert.EnterMultipleScope())
        {
            // Assert
            Assert.That(result.Format, Is.EqualTo("png"));
            Assert.That(result.CompressedSize, Is.GreaterThan(0));
        }
    }

    [Test]
    public void CompressImage_UnsupportedExtension_ThrowsRpcException()
    {
        // Arrange
        var imageData = CreateTestImage();
        var request = new CompressionRequest
        {
            FileName = "test.bmp",
            ImageData = ByteString.CopyFrom(imageData)
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<RpcException>(async () =>
            await service.CompressImage(request, contextMock.Object));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
            Assert.That(ex.Status.Detail, Does.Contain("not supported"));
        }
    }

    [Test]
    public void CompressImage_TooLargeFile_ThrowsRpcException()
    {
        // Arrange
        var bigData = new byte[1024 * 1024 * 10];
        var request = new CompressionRequest
        {
            FileName = "big.jpg",
            ImageData = ByteString.CopyFrom(bigData)
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<RpcException>(async () =>
            await service.CompressImage(request, contextMock.Object));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
            Assert.That(ex.Status.Detail, Does.Contain("File size exceeds"));
        }
    }

    [Test]
    public void CompressImage_EmptyData_ThrowsRpcException()
    {
        // Arrange
        var request = new CompressionRequest
        {
            FileName = "empty.jpg",
            ImageData = ByteString.Empty
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<RpcException>(async () =>
            await service.CompressImage(request, contextMock.Object));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
            Assert.That(ex.Status.Detail, Does.Contain("cannot be empty"));
        }
    }
}