namespace SceneSplit.ImageCompression.Api;

internal static partial class Log
{
    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Received compression request: File='{FileName}', Size={Size} bytes, ResizeWidth={ResizeWidth}, ResizeHeight={ResizeHeight}, KeepAspectRatio={KeepAspectRatio}, Quality={Quality}")]
    public static partial void ReceivedCompressionRequest(
        ILogger logger, string fileName, int size, int resizeWidth, int resizeHeight, bool keepAspectRatio, int quality);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Invalid request: image data is empty.")]
    public static partial void EmptyImageData(ILogger logger);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Invalid request: file size {ActualSize} exceeds limit {Limit} bytes.")]
    public static partial void FileTooLarge(ILogger logger, int actualSize, int limit);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning, Message = "Invalid request: file extension '{Extension}' is not supported. Allowed: {AllowedList}")]
    public static partial void UnsupportedExtension(ILogger logger, string extension, string allowedList);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information,
        Message = "Resizing image from {OriginalWidth}x{OriginalHeight} to {NewWidth}x{NewHeight}")]
    public static partial void ResizingImage(ILogger logger, int originalWidth, int originalHeight, int newWidth, int newHeight);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "Skipping resize: no resize requested or dimensions unchanged.")]
    public static partial void SkippingResize(ILogger logger);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information,
        Message = "Compression completed for '{FileName}': OriginalSize={OriginalSize} bytes, CompressedSize={CompressedSize} bytes, Output={Width}x{Height}, Quality={Quality}")]
    public static partial void CompressionCompleted(
        ILogger logger, string fileName, int originalSize, int compressedSize, int width, int height, int quality);
}