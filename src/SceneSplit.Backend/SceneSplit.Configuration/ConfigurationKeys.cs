namespace SceneSplit.Configuration;

public static class ApiConfigurationKeys
{
    public static string ALLOWED_CORS_ORIGINS { get; } = nameof(ALLOWED_CORS_ORIGINS);
    public static string ALLOWED_IMAGE_TYPES { get; } = nameof(ALLOWED_IMAGE_TYPES);
    public static string MAX_IMAGE_SIZE { get; } = nameof(MAX_IMAGE_SIZE);
}