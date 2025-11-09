using Google.Protobuf;
using Grpc.Core;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Api.Helpers;
using SceneSplit.ImageCompression.Sdk;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace SceneSplit.ImageCompression.Api.Services;

public class CompressionService : Compression.CompressionBase
{
    private const int DEFAULT_QUALITY = 75;

    private readonly string[] allowedImageTypes;
    private readonly int maxFileSizeInBytes;
    private readonly ILogger<CompressionService> logger;

    public CompressionService(IConfiguration configuration, ILogger<CompressionService> logger)
    {
        var allowedImageTypesConfig = configuration[ImageCompressionApiConfigurationKeys.ALLOWED_IMAGE_TYPES] ?? ".jpg,.jpeg,.png";
        var maxFileSizeInBytesConfig = configuration[ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE] ?? SizeConversionHelper.ToBytes(10).ToString();

        allowedImageTypes = allowedImageTypesConfig.Split(',');
        maxFileSizeInBytes = int.Parse(maxFileSizeInBytesConfig);
        this.logger = logger;
    }

    public override async Task<CompressionReply> CompressImage(CompressionRequest request, ServerCallContext context)
    {
        ValidateRequest(request);

        var quality = request.Quality > 0 ? request.Quality : DEFAULT_QUALITY;
        Log.ReceivedCompressionRequest(logger, request.FileName, request.ImageData.Length, request.ResizeWidth, request.ResizeHeight, request.KeepAspectRatio, quality);

        using var input = new MemoryStream(request.ImageData.ToByteArray());
        using var image = await Image.LoadAsync(input, context.CancellationToken);

        var originalWidth = image.Width;
        var originalHeight = image.Height;

        var resized = ResizeImageIfNeeded(image, request);

        if (resized.Width != originalWidth || resized.Height != originalHeight)
        {
            Log.ResizingImage(logger, originalWidth, originalHeight, resized.Width, resized.Height);
        }
        else
        {
            Log.SkippingResize(logger);
        }

        using var output = new MemoryStream();

        await resized.SaveAsync(output, new JpegEncoder
        {
            Quality = quality
        }, context.CancellationToken);

        var compressedBytes = output.ToArray();

        Log.CompressionCompleted(logger, request.FileName, request.ImageData.Length, compressedBytes.Length, resized.Width, resized.Height, quality);

        return new CompressionReply
        {
            CompressedImage = ByteString.CopyFrom(compressedBytes),
            Format = "jpg",
            OriginalSize = request.ImageData.Length,
            CompressedSize = compressedBytes.Length,
            NewWidth = resized.Width,
            NewHeight = resized.Height
        };
    }

    private static Image ResizeImageIfNeeded(Image image, CompressionRequest request)
    {
        if (request.ResizeWidth <= 0 && request.ResizeHeight <= 0)
        {
            return image;
        }

        int newWidth = request.ResizeWidth;
        int newHeight = request.ResizeHeight;

        if (request.KeepAspectRatio)
        {
            var aspectRatio = (double)image.Width / image.Height;

            if (newWidth > 0 && newHeight == 0)
            {
                newHeight = (int)(newWidth / aspectRatio);
            }
            else if (newHeight > 0 && newWidth == 0)
            {
                newWidth = (int)(newHeight * aspectRatio);
            }
            else if (newWidth == 0 && newHeight == 0)
            {
                return image;
            }
        }

        newWidth = Math.Clamp(newWidth > 0 ? newWidth : image.Width, 1, image.Width);
        newHeight = Math.Clamp(newHeight > 0 ? newHeight : image.Height, 1, image.Height);

        image.Mutate(x => x.Resize(newWidth, newHeight));
        return image;
    }

    private void ValidateRequest(CompressionRequest request)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
        {
            Log.EmptyImageData(logger);
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Image data cannot be empty."));
        }

        if (request.ImageData.Length > maxFileSizeInBytes)
        {
            Log.FileTooLarge(logger, request.ImageData.Length, maxFileSizeInBytes);
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"File size exceeds {SizeConversionHelper.ToMB(maxFileSizeInBytes)}MB limit."));
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedImageTypes.Contains(extension))
        {
            Log.UnsupportedExtension(logger, extension, string.Join(',', allowedImageTypes));
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"File extension '{extension}' is not supported."));
        }
    }
}