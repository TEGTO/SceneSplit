using Grpc.Core;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Sdk;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace SceneSplit.ImageCompression.Api.Services;

public class CompressionService : Compression.CompressionBase
{
    private const int DEFAULT_QUALITY = 75;

    private readonly string[] allowedImageTypes;
    private readonly int maxFileSizeInBytes;

    public CompressionService(IConfiguration configuration)
    {
        var allowedImageTypesConfig = configuration[ImageCompressionApiConfigurationKeys.ALLOWED_IMAGE_TYPES] ?? ".jpg,.jpeg,.png";
        var maxFileSizeInBytesConfig = configuration[ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE] ?? ToBytes(10).ToString();

        allowedImageTypes = allowedImageTypesConfig.Split(',');

        maxFileSizeInBytes = int.Parse(maxFileSizeInBytesConfig);
    }

    public override async Task<CompressionReply> CompressImage(CompressionRequest request, ServerCallContext context)
    {
        ValidateRequest(request);

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        var quality = request.Quality > 0 ? request.Quality : DEFAULT_QUALITY;

        using var input = new MemoryStream(request.ImageData.ToByteArray());
        using var image = await Image.LoadAsync(input, context.CancellationToken);
        using var output = new MemoryStream();

        if (extension == ".png")
        {
            await image.SaveAsync(output, new PngEncoder
            {
                CompressionLevel = MapQualityToPngCompressionLevel(quality),
                FilterMethod = PngFilterMethod.Adaptive
            }, context.CancellationToken);
        }
        else
        {
            await image.SaveAsync(output, new JpegEncoder { Quality = quality }, context.CancellationToken);
        }

        var compressedBytes = output.ToArray();

        return new CompressionReply
        {
            CompressedImage = Google.Protobuf.ByteString.CopyFrom(compressedBytes),
            Format = extension.TrimStart('.'),
            OriginalSize = request.ImageData.Length,
            CompressedSize = compressedBytes.Length
        };
    }

    private void ValidateRequest(CompressionRequest request)
    {
        if (request.ImageData == null || request.ImageData.Length == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Image data cannot be empty."));
        }

        if (request.ImageData.Length > maxFileSizeInBytes)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"File size exceeds {ToMB(maxFileSizeInBytes)}MB limit."));
        }

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedImageTypes.Contains(extension))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"File extension '{extension}' is not supported."));
        }
    }

    private static PngCompressionLevel MapQualityToPngCompressionLevel(int quality)
    {
        var level = Math.Clamp((int)Math.Round((100 - quality) / 11.0), 0, 9);
        return level switch
        {
            0 => PngCompressionLevel.Level0,
            1 => PngCompressionLevel.Level1,
            2 => PngCompressionLevel.Level2,
            3 => PngCompressionLevel.Level3,
            4 => PngCompressionLevel.Level4,
            5 => PngCompressionLevel.Level5,
            6 => PngCompressionLevel.Level6,
            7 => PngCompressionLevel.Level7,
            8 => PngCompressionLevel.Level8,
            _ => PngCompressionLevel.Level9
        };
    }

    private static int ToMB(int bytes) => bytes / (1024 * 1024);
    private static int ToBytes(int mb) => mb * 1024 * 1024;
}