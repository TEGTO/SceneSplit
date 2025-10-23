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
            Quality = 10
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        Assert.That(result, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.CompressedImage.Length, Is.LessThan(request.ImageData.Length));
            Assert.That(result.Format, Is.EqualTo("jpg"));
        }
    }

    [Test]
    public async Task CompressImage_ValidPngImage_ReturnsCompressedAsJpg()
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

        Assert.Multiple(() =>
        {
            Assert.That(result.Format, Is.EqualTo("jpg"));
            Assert.That(result.CompressedSize, Is.GreaterThan(0));
        });
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
        var ex = Assert.ThrowsAsync<RpcException>(() =>
            service.CompressImage(request, contextMock.Object));

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
        var ex = Assert.ThrowsAsync<RpcException>(() =>
            service.CompressImage(request, contextMock.Object));

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
        var ex = Assert.ThrowsAsync<RpcException>(() =>
            service.CompressImage(request, contextMock.Object));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.StatusCode, Is.EqualTo(StatusCode.InvalidArgument));
            Assert.That(ex.Status.Detail, Does.Contain("cannot be empty"));
        }
    }

    [Test]
    public async Task CompressImage_ResizeToExactDimensions_ResizesCorrectly()
    {
        // Arrange
        var imageData = CreateTestImage(400, 200);
        var request = new CompressionRequest
        {
            FileName = "resize.jpg",
            ImageData = ByteString.CopyFrom(imageData),
            ResizeWidth = 100,
            ResizeHeight = 100
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.NewWidth, Is.EqualTo(100));
            Assert.That(result.NewHeight, Is.EqualTo(100));
        }
    }

    [Test]
    public async Task CompressImage_ResizeWithKeepAspectRatioWidthOnly_ResizesProportionally()
    {
        // Arrange
        var imageData = CreateTestImage(400, 200);
        var request = new CompressionRequest
        {
            FileName = "resize_aspect.jpg",
            ImageData = ByteString.CopyFrom(imageData),
            ResizeWidth = 200,
            KeepAspectRatio = true
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.NewWidth, Is.EqualTo(200));
            Assert.That(result.NewHeight, Is.EqualTo(100));
        }
    }

    [Test]
    public async Task CompressImage_ResizeWithKeepAspectRatioHeightOnly_ResizesProportionally()
    {
        // Arrange
        var imageData = CreateTestImage(300, 150);
        var request = new CompressionRequest
        {
            FileName = "resize_height.jpg",
            ImageData = ByteString.CopyFrom(imageData),
            ResizeHeight = 75,
            KeepAspectRatio = true
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.NewHeight, Is.EqualTo(75));
            Assert.That(result.NewWidth, Is.EqualTo(150));
        }
    }

    [Test]
    public async Task CompressImage_NoResizeParams_KeepsOriginalDimensions()
    {
        // Arrange  
        var imageData = CreateTestImage(120, 80);
        var request = new CompressionRequest
        {
            FileName = "noresize.jpg",
            ImageData = ByteString.CopyFrom(imageData)
        };

        // Act
        var result = await service.CompressImage(request, contextMock.Object);

        // Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.NewWidth, Is.EqualTo(120));
            Assert.That(result.NewHeight, Is.EqualTo(80));
        }
    }
}