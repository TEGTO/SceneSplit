using Amazon.CDK;

namespace SceneSplit.Cdk;

internal class SceneSplitStackProps : StackProps
{
    public required string FrontendDockerfileDirectory { get; init; }
    public required string FrontendDockerfileName { get; init; }
}
