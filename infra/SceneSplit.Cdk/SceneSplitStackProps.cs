using Amazon.CDK;

namespace SceneSplit.Cdk;

internal class SceneSplitStackProps : StackProps
{
    public string ApiHubEndpoint { get; set; } = string.Empty;
    public string ApiDockerfileDirectory { get; set; } = string.Empty;
    public string ApiDockerfileName { get; set; } = string.Empty;
    public Dictionary<string, string> ApiConfigurationVariables { get; set; } = [];

    public string FrontendDockerfileDirectory { get; set; } = string.Empty;
    public string FrontendDockerfileName { get; set; } = string.Empty;
    public Dictionary<string, string> FrontendConfigurationVariables { get; set; } = [];
}