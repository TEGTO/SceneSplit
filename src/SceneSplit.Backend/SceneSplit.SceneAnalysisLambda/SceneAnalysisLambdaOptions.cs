using Amazon.Lambda.Core;
using SceneSplit.Configuration;
using SceneSplit.LambdaShared;

namespace SceneSplit.SceneAnalysisLambda;

public record SceneAnalysisLambdaOptions
{
    public string SqsQueueUrl { get; init; } = default!;
    public string BedrockModelId { get; init; } = "amazon.nova-lite-v1:0";
    public int MaxItems { get; init; } = 5;

    public static SceneAnalysisLambdaOptions FromEnvironment(ILambdaContext context)
    {
        var model = Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.BEDROCK_MODEL)
            ?? throw new InvalidOperationException($"{SceneAnalysisLambdaConfigurationKeys.BEDROCK_MODEL} is missing");

        (string regin, string accountId) = LambdaHelper.GetRegionAndAccountId(context);
        var modelId = $"arn:aws:bedrock:{regin}:{accountId}:inference-profile/{model}";

        return new()
        {
            SqsQueueUrl = Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.SQS_QUEUE_URL)
                ?? throw new InvalidOperationException($"{SceneAnalysisLambdaConfigurationKeys.SQS_QUEUE_URL} is missing"),
            BedrockModelId = modelId,
            MaxItems = int.TryParse(Environment.GetEnvironmentVariable(SceneAnalysisLambdaConfigurationKeys.MAX_ITEMS), out var count)
                ? count : 5
        };
    }
}