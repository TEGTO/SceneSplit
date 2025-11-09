using SceneSplit.Configuration;

namespace SceneSplit.ObjectImageSearchLambda;

public record ObjectImageSearchLambdaOptions
{
    public string BucketName { get; init; } = default!;
    public string ImageSearchApiKey { get; init; } = default!;
    public string ImageSearchApiEndpoint { get; init; } = default!;
    public string CompressionApiUrl { get; init; } = default!;
    public int MaxImageSize { get; init; } = default!;
    public int ResizeWidth { get; init; } = default!;
    public int ResizeHeight { get; init; } = default!;
    public int ImageQualityCompression { get; init; } = default!;

    public static ObjectImageSearchLambdaOptions FromEnvironment() => new()
    {
        BucketName = Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.BUCKET_NAME)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.BUCKET_NAME} is missing"),

        ImageSearchApiKey = Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_KEY)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_KEY} is missing"),

        ImageSearchApiEndpoint = Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_ENDPOINT)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_ENDPOINT} is missing"),

        CompressionApiUrl = Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.COMPRESSION_API_URL)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.COMPRESSION_API_URL} is missing"),

        MaxImageSize = int.Parse(Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.MAX_IMAGE_SIZE)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.MAX_IMAGE_SIZE} is missing")),

        ResizeWidth = int.Parse(Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.RESIZE_WIDTH)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.RESIZE_WIDTH} is missing")),

        ResizeHeight = int.Parse(Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.RESIZE_HEIGHT)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.RESIZE_HEIGHT} is missing")),

        ImageQualityCompression = int.Parse(Environment.GetEnvironmentVariable(ObjectImageSearchLambdaConfigurationKeys.IMAGE_QUALITY_COMPRESSION)
            ?? throw new InvalidOperationException($"{ObjectImageSearchLambdaConfigurationKeys.IMAGE_QUALITY_COMPRESSION} is missing")),
    };
}