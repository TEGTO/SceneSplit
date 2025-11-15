using Microsoft.Extensions.Logging;

namespace SceneSplit.ObjectImageCleanerLambda;

internal static partial class Log
{
    [LoggerMessage(EventId = 5000, Level = LogLevel.Information, Message = "Cleaner triggered for {Bucket}/{Key}")]
    public static partial void CleanerTriggered(ILogger logger, string bucket, string key);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning, Message = "Skipping cleanup for {Bucket}/{Key} due to missing tags. userId='{UserId}', workflowId='{WorkflowId}'")]
    public static partial void SkippingCleanupMissingTags(ILogger logger, string bucket, string key, string? userId, string? workflowId);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Starting cleanup. UserId={UserId}, Keep WorkflowId={WorkflowId}")]
    public static partial void StartingCleanup(ILogger logger, string userId, string workflowId);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Warning, Message = "Failed to read tags for {Bucket}/{Key}. Skipping.")]
    public static partial void FailedToReadTags(ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Deleted batch of {Count} objects from bucket {Bucket}")]
    public static partial void DeletedBatch(ILogger logger, int count, string bucket);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Warning, Message = "Delete error: Key={Key}, Code={Code}, Message={Message}")]
    public static partial void DeleteError(ILogger logger, string key, string code, string message);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Information, Message = "Cleanup completed for UserId={UserId}. Scanned={Scanned}, Deleted={Deleted}, KeptWorkflowId={WorkflowId}")]
    public static partial void CleanupCompleted(ILogger logger, string userId, int scanned, int deleted, string workflowId);
}