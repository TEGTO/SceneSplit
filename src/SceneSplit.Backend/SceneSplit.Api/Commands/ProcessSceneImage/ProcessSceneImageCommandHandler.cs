using MediatR;
using Microsoft.AspNetCore.SignalR;
using SceneSplit.Api.Services.StorageService;
using SceneSplit.Configuration;
using SceneSplit.ImageCompression.Sdk;

namespace SceneSplit.Api.Commands.ProcessSceneImage;

public class ProcessSceneImageCommandHandler : IRequestHandler<ProcessSceneImageCommand>
{
    private readonly Compression.CompressionClient compressionClient;
    private readonly IStorageService storageService;
    private readonly string[] allowedImageTypes;
    private readonly int maxFileSizeInBytes;
    private readonly int imageQuality;
    private readonly int resizeWidth;
    private readonly int resizeHeight;

    public ProcessSceneImageCommandHandler(
        Compression.CompressionClient compressionClient,
        IStorageService storageService,
        IConfiguration configuration)
    {
        this.compressionClient = compressionClient;
        this.storageService = storageService;

        var allowedImageTypesConfig = configuration[ApiConfigurationKeys.ALLOWED_IMAGE_TYPES] ?? ".jpg,.jpeg,.png";
        var maxFileSizeInBytesConfig = configuration[ApiConfigurationKeys.MAX_IMAGE_SIZE] ?? ToBytes(10).ToString();
        var qualityConfig = configuration[ApiConfigurationKeys.IMAGE_QUALITY_COMPRESSION] ?? "75";
        var resizeWidthConfig = configuration[ApiConfigurationKeys.RESIZE_WIDTH] ?? "1024";
        var resizeHeightConfig = configuration[ApiConfigurationKeys.RESIZE_HEIGHT] ?? "1024";

        allowedImageTypes = allowedImageTypesConfig.Split(",");
        maxFileSizeInBytes = int.Parse(maxFileSizeInBytesConfig);
        imageQuality = int.Parse(qualityConfig);
        resizeWidth = int.Parse(resizeWidthConfig);
        resizeHeight = int.Parse(resizeHeightConfig);
    }

    public async Task Handle(ProcessSceneImageCommand request, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedImageTypes.Contains(extension))
        {
            throw new HubException("File extension is not supported.");
        }

        if (request.FileContent.Length > maxFileSizeInBytes)
        {
            throw new HubException($"File size exceeds {ToMB(maxFileSizeInBytes)}MB limit.");
        }

        var compressionRequest = new CompressionRequest
        {
            FileName = request.FileName,
            ImageData = Google.Protobuf.ByteString.CopyFrom(request.FileContent),
            Quality = imageQuality,
            ResizeWidth = resizeWidth,
            ResizeHeight = resizeHeight,
            KeepAspectRatio = true
        };

        var response = await compressionClient.CompressImageAsync(compressionRequest, cancellationToken: cancellationToken);

        await storageService.UploadSceneImageAsync(
            request.FileName,
            response.CompressedImage.ToByteArray(),
            $"image/{response.Format}",
            request.UserId,
            cancellationToken);
    }

    private static int ToMB(int bytes) => bytes / (1024 * 1024);
    private static int ToBytes(int mb) => mb * 1024 * 1024;
}