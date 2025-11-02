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
    private readonly ILogger<ProcessSceneImageCommandHandler> logger;

    public ProcessSceneImageCommandHandler(
        Compression.CompressionClient compressionClient,
        IStorageService storageService,
        IConfiguration configuration,
        ILogger<ProcessSceneImageCommandHandler> logger)
    {
        this.compressionClient = compressionClient;
        this.storageService = storageService;
        this.logger = logger;

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
        Log.ProcessingSceneImage(logger, request.UserId, request.FileName, request.FileContent.Length);

        var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedImageTypes.Contains(extension))
        {
            Log.UnsupportedExtension(logger, extension, request.FileName, string.Join(",", allowedImageTypes));
            throw new HubException("File extension is not supported.");
        }

        if (request.FileContent.Length > maxFileSizeInBytes)
        {
            Log.FileTooLarge(logger, request.FileContent.Length, maxFileSizeInBytes, request.FileName);
            throw new HubException($"File size exceeds {ToMB(maxFileSizeInBytes)}MB limit.");
        }

        var tempFileName = $"{Guid.NewGuid()}{extension}";

        var compressionRequest = new CompressionRequest
        {
            FileName = tempFileName,
            ImageData = Google.Protobuf.ByteString.CopyFrom(request.FileContent),
            Quality = imageQuality,
            ResizeWidth = resizeWidth,
            ResizeHeight = resizeHeight,
            KeepAspectRatio = true
        };

        Log.SendingCompressionRequest(logger, tempFileName, imageQuality, resizeWidth, resizeHeight, true);

        var response = await compressionClient.CompressImageAsync(compressionRequest, cancellationToken: cancellationToken);

        Log.ImageCompressed(logger, tempFileName, compressionRequest.ImageData.Length, response.CompressedImage.Length, response.Format);

        var newFileName = $"{Path.GetFileNameWithoutExtension(tempFileName)}.{response.Format}";
        var contentType = $"image/{response.Format}";

        Log.UploadingToStorage(logger, newFileName, request.UserId, contentType);

        await storageService.UploadSceneImageAsync(
            newFileName,
            response.CompressedImage.ToByteArray(),
            contentType,
            request.UserId,
            cancellationToken);

        Log.UploadCompleted(logger, newFileName, request.UserId);
    }

    private static int ToMB(int bytes) => bytes / (1024 * 1024);
    private static int ToBytes(int mb) => mb * 1024 * 1024;
}