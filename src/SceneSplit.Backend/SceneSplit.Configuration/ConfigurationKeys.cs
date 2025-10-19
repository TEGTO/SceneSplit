namespace SceneSplit.Configuration;

public static class ApiConfigurationKeys
{
    public static string ALLOWED_CORS_ORIGINS { get; } = nameof(ALLOWED_CORS_ORIGINS);
    public static string ALLOWED_IMAGE_TYPES { get; } = nameof(ALLOWED_IMAGE_TYPES);
    public static string MAX_IMAGE_SIZE { get; } = nameof(MAX_IMAGE_SIZE);
    public static string COMPRESSION_API_URL { get; } = nameof(COMPRESSION_API_URL);
    public static string SCENE_IMAGE_BUCKET { get; } = nameof(SCENE_IMAGE_BUCKET);
    public static string IMAGE_QUALITY_COMPRESSION { get; } = nameof(IMAGE_QUALITY_COMPRESSION);
    public static string RESIZE_WIDTH { get; } = nameof(RESIZE_WIDTH);
    public static string RESIZE_HEIGHT { get; } = nameof(RESIZE_HEIGHT);
}

public static class WorkflowTags
{
    public const string USER_ID_TAG = "UserId";
    public const string WORKFLOW_ID = "WorkflowId";

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
    public static string BEDROCK_MODEL_ID { get; } = nameof(BEDROCK_MODEL_ID);
}