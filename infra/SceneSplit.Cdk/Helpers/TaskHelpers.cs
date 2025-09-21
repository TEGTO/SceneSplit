using Amazon.CDK;
using Amazon.CDK.AWS.ECS;

namespace SceneSplit.Cdk.Helpers;

public static class TaskHelpers
{
    public static HealthCheck AddHealthCheckForTask(string relativeUrl)
    {
        return new HealthCheck
        {
            Command = ["CMD-SHELL", $"curl -f http://localhost:{relativeUrl} || exit 1"],
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            Retries = 3,
            StartPeriod = Duration.Seconds(10)
        };
    }
}