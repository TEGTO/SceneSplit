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

    [LoggerMessage(EventId = 4200, Level = LogLevel.Information, Message = "Fetching images for UserId={UserId} from bucket {Bucket}")]
    public static partial void FetchingUserImages(ILogger logger, string userId, string bucket);

    [LoggerMessage(EventId = 4201, Level = LogLevel.Information, Message = "No images found for UserId={UserId}")]
    public static partial void NoImagesFoundForUser(ILogger logger, string userId);

    [LoggerMessage(EventId = 4202, Level = LogLevel.Information, Message = "Latest workflow for UserId={UserId} resolved to WorkflowId={WorkflowId}")]
    public static partial void LatestWorkflowResolved(ILogger logger, string userId, string workflowId);

    [LoggerMessage(EventId = 4203, Level = LogLevel.Information, Message = "Returning {Count} images for UserId={UserId} WorkflowId={WorkflowId}")]
    public static partial void ReturningImagesForWorkflow(ILogger logger, int count, string userId, string workflowId);

    [LoggerMessage(EventId = 5010, Level = LogLevel.Information, Message = "S3ObjectImageWatcher starting. Bucket={Bucket} IntervalSeconds={IntervalSeconds}")]
    public static partial void S3ObjectImageWatcherStarting(ILogger logger, string bucket, int intervalSeconds);

    [LoggerMessage(EventId = 5011, Level = LogLevel.Information, Message = "S3ObjectImageWatcher detected {UserCount} users with new images")]
    public static partial void S3ObjectImageWatcherDetectedUsers(ILogger logger, int userCount);

    [LoggerMessage(EventId = 5012, Level = LogLevel.Information, Message = "S3ObjectImageWatcher pushed updated images for UserId={UserId}")]
    public static partial void S3ObjectImageWatcherPushedUser(ILogger logger, string userId);

    [LoggerMessage(EventId = 5013, Level = LogLevel.Error, Message = "S3ObjectImageWatcher polling error")]
    public static partial void S3ObjectImageWatcherPollingError(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5014, Level = LogLevel.Information, Message = "S3ObjectImageWatcher cancellation requested (graceful shutdown).")]
    public static partial void S3ObjectImageWatcherCancellation(ILogger logger);

    [LoggerMessage(EventId = 5015, Level = LogLevel.Information, Message = "S3ObjectImageWatcher stopped.")]
    public static partial void S3ObjectImageWatcherStopped(ILogger logger);

    [LoggerMessage(EventId = 5016, Level = LogLevel.Information, Message = "S3ObjectImageWatcher refresh started for UserId={UserId}")]
    public static partial void S3ObjectImageWatcherRefreshStarted(ILogger logger, string userId);

    [LoggerMessage(EventId = 5017, Level = LogLevel.Information, Message = "S3ObjectImageWatcher refresh canceled for UserId={UserId}")]
    public static partial void S3ObjectImageWatcherRefreshCanceled(ILogger logger, string userId);

    [LoggerMessage(EventId = 5018, Level = LogLevel.Error, Message = "S3ObjectImageWatcher refresh error for UserId={UserId}")]
    public static partial void S3ObjectImageWatcherRefreshError(ILogger logger, Exception exception, string userId);
}