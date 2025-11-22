using Amazon.Lambda.Core;

namespace SceneSplit.LambdaShared;

public static class LambdaHelper
{
    public static (string region, string accountId) GetRegionAndAccountId(ILambdaContext context)
    {
        var arnParts = context.InvokedFunctionArn.Split(':');

        var region = arnParts[3];
        var accountId = arnParts[4];

        return (region, accountId);
    }
}