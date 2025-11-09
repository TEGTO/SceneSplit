using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace SceneSplit.SceneAnalysisLambda;

internal static partial class Log
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information, Message = "Processing S3 object: {Bucket}/{Key}")]
    public static partial void ProcessingS3Object(ILogger logger, string bucket, string key);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "No items detected in {Key}")]
    public static partial void NoItemsDetected(ILogger logger, string key);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Failed to process image {Bucket}/{Key}")]
    public static partial void FailedToProcessImage(ILogger logger, Exception exception, string bucket, string key);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Published analysis result to SQS: {Body}")]
    public static partial void PublishedAnalysisResult(ILogger logger, string body);
}