using SceneSplit.Configuration;

namespace SceneSplit.ObjectImageCleanerLambda;

public record ObjectImageCleanerLambdaOptions
{
    public int MaxKeysSearchConcurrency { get; init; } = 1000;
    public int MaxDeleteBatch { get; init; } = 16;

    public static ObjectImageCleanerLambdaOptions FromEnvironment() => new()
    {
        MaxKeysSearchConcurrency = int.Parse(Environment.GetEnvironmentVariable(ObjectImageCleanerLambdaConfigurationKeys.MAX_KEYS_SEARCH_CONCURENCY)
            ?? 1000.ToString()),

        MaxDeleteBatch = int.Parse(Environment.GetEnvironmentVariable(ObjectImageCleanerLambdaConfigurationKeys.MAX_DELETE_BRANCH)
            ?? 16.ToString()),
    };
}