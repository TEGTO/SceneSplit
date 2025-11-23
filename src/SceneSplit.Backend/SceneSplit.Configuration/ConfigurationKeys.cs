namespace SceneSplit.Configuration;

public static class ApiConfigurationKeys
{
    public static string ALLOWED_CORS_ORIGINS { get; } = nameof(ALLOWED_CORS_ORIGINS);
    public static string ALLOWED_IMAGE_TYPES { get; } = nameof(ALLOWED_IMAGE_TYPES);
    public static string MAX_IMAGE_SIZE { get; } = nameof(MAX_IMAGE_SIZE);
    public static string COMPRESSION_API_URL { get; } = nameof(COMPRESSION_API_URL);
    public static string SCENE_IMAGE_BUCKET { get; } = nameof(SCENE_IMAGE_BUCKET);
    public static string OBJECT_IMAGE_BUCKET { get; } = nameof(OBJECT_IMAGE_BUCKET);
    public static string OBJECT_IMAGE_POLL_INTERVAL_SECONDS { get; } = nameof(OBJECT_IMAGE_POLL_INTERVAL_SECONDS);
    public static string IMAGE_QUALITY_COMPRESSION { get; } = nameof(IMAGE_QUALITY_COMPRESSION);
    public static string RESIZE_WIDTH { get; } = nameof(RESIZE_WIDTH);
    public static string RESIZE_HEIGHT { get; } = nameof(RESIZE_HEIGHT);
}

public static class WorkflowTags
{
    public const string USER_ID_TAG = "UserId";
    public const string WORKFLOW_ID = "WorkflowId";
    public const string DESCRIPTION = "Description";

    public const string UNKNOWN = "unknown";
    public static readonly IEnumerable<string> ALL_TAGS = [USER_ID_TAG, WORKFLOW_ID];
}

public static class ImageCompressionApiConfigurationKeys
{
    public static string ALLOWED_IMAGE_TYPES { get; } = nameof(ALLOWED_IMAGE_TYPES);
    public static string MAX_IMAGE_SIZE { get; } = nameof(MAX_IMAGE_SIZE);
}

public static class SceneAnalysisLambdaConfigurationKeys
{
    public static string SQS_QUEUE_URL { get; } = nameof(SQS_QUEUE_URL);
    public static string MAX_ITEMS { get; } = nameof(MAX_ITEMS);
    public static string BEDROCK_MODEL { get; } = nameof(BEDROCK_MODEL);
}

public static class ObjectImageSearchLambdaConfigurationKeys
{
    public static string BUCKET_NAME { get; } = nameof(BUCKET_NAME);
    public static string IMAGE_SEARCH_API_KEY { get; } = nameof(IMAGE_SEARCH_API_KEY);
    public static string IMAGE_SEARCH_API_ENDPOINT { get; } = nameof(IMAGE_SEARCH_API_ENDPOINT);
    public static string COMPRESSION_API_URL { get; } = nameof(COMPRESSION_API_URL);
    public static string MAX_IMAGE_SIZE { get; } = nameof(MAX_IMAGE_SIZE);
    public static string RESIZE_WIDTH { get; } = nameof(RESIZE_WIDTH);
    public static string RESIZE_HEIGHT { get; } = nameof(RESIZE_HEIGHT);
    public static string IMAGE_QUALITY_COMPRESSION { get; } = nameof(IMAGE_QUALITY_COMPRESSION);
}

public static class ObjectImageCleanerLambdaConfigurationKeys
{
    public static string MAX_KEYS_SEARCH_CONCURENCY { get; } = nameof(MAX_KEYS_SEARCH_CONCURENCY);
    public static string MAX_DELETE_BRANCH { get; } = nameof(MAX_DELETE_BRANCH);
}

public static class ObservabilityConfigurationKeys
{
    public static string OPENTELEMETRY_SERVICE_NAMESPACE { get; } = nameof(OPENTELEMETRY_SERVICE_NAMESPACE);
    public static string OPENTELEMETRY_SERVICE_NAME { get; } = nameof(OPENTELEMETRY_SERVICE_NAME);
    public static string HEALTH_ENDPOINT { get; } = "/health";
}