namespace SceneSplit.Api;

internal static partial class Log
{
    [LoggerMessage(EventId = 4000, Level = LogLevel.Information, Message = "Processing scene image. UserId={UserId}, File='{FileName}', Size={Size} bytes")]
    public static partial void ProcessingSceneImage(ILogger logger, string userId, string fileName, int size);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "Unsupported file extension '{Extension}' for File='{FileName}'. Allowed: {AllowedList}")]
    public static partial void UnsupportedExtension(ILogger logger, string extension, string fileName, string allowedList);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Warning, Message = "File too large. Size={ActualSize} bytes exceeds limit {Limit} bytes for File='{FileName}'")]
    public static partial void FileTooLarge(ILogger logger, int actualSize, int limit, string fileName);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Information, Message = "Sending compression request for '{FileName}' with quality={Quality}, resize={ResizeWidth}x{ResizeHeight} (KeepAspectRatio={KeepAspectRatio})")]
    public static partial void SendingCompressionRequest(ILogger logger, string fileName, int quality, int resizeWidth, int resizeHeight, bool keepAspectRatio);

    [LoggerMessage(EventId = 4004, Level = LogLevel.Information, Message = "Compression completed for '{FileName}'. Original={OriginalSize} bytes, Compressed={CompressedSize} bytes, Format={Format}")]
    public static partial void ImageCompressed(ILogger logger, string fileName, int originalSize, int compressedSize, string format);

    [LoggerMessage(EventId = 4005, Level = LogLevel.Information, Message = "Uploading '{FileName}' to storage for UserId={UserId} with ContentType={ContentType}")]
    public static partial void UploadingToStorage(ILogger logger, string fileName, string userId, string contentType);

    [LoggerMessage(EventId = 4006, Level = LogLevel.Information, Message = "Uploaded '{FileName}' for UserId={UserId}")]
    public static partial void UploadCompleted(ILogger logger, string fileName, string userId);
}