using Microsoft.Extensions.Logging;

namespace SceneSplit.ObjectImageSearchLambda;

internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Processing SQS message ID: {MessageId}")]
    public static partial void ProcessingMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Skipping invalid or empty message body for message ID {MessageId}")]
    public static partial void SkippingInvalidMessage(ILogger logger, string messageId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Failed to process SQS message ID {MessageId}. Exception: {ExceptionMessage}")]
    public static partial void FailedToProcessWithException(ILogger logger, Exception exception, string messageId, string exceptionMessage);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Searching images for '{Item}'")]
    public static partial void SearchingImages(ILogger logger, string item);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "No images found for '{Item}'")]
    public static partial void NoImagesFound(ILogger logger, string item);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error, Message = "Failed to process SQS message ID {MessageId}")]
    public static partial void FailedToProcess(ILogger logger, Exception exception, string messageId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Successfully processed SQS message ID: {MessageId}")]
    public static partial void ProcessedSuccessfully(ILogger logger, string messageId);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Downloading image: {ImageUrl}")]
    public static partial void DownloadingImage(ILogger logger, string imageUrl);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Uploaded {FileName} to {BucketName}")]
    public static partial void UploadedToBucket(ILogger logger, string fileName, string bucketName);
}
