using SceneSplit.Configuration;

namespace SceneSplit.SceneAnalysisLambda;

public record SceneAnalysisLambdaOptions
{
    public string SqsQueueUrl { get; init; } = default!;
    public string BedrockModelId { get; init; } = "amazon.nova-lite-v1:0";
    public int MaxItems { get; init; } = 5;

    public static SceneAnalysisLambdaOptions FromEnvironment() => new()
    {
        SqsQueueUrl = Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.SQS_QUEUE_URL)
            ?? throw new InvalidOperationException($"{SceneAnalysisLambdaConfigurationKeys.SQS_QUEUE_URL} is missing"),

        BedrockModelId = Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.BEDROCK_MODEL_ID)
            ?? "amazon.nova-lite-v1:0",

        MaxItems = int.TryParse(Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.MAX_ITEMS), out var count)
            ? count : 5
    };
}