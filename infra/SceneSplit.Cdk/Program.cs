using Amazon.CDK;

namespace SceneSplit.Cdk;

static class Program
{
    public static void Main()
    {
        var app = new App();

        var props = new SceneSplitStackProps
        {
            FrontendDockerfileDirectory = "src/SceneSplit.Frontend",
            FrontendDockerfileName = "Dockerfile"
        };

        _ = new SceneSplitStack(app, nameof(SceneSplitStack), props);
        app.Synth();
    }
}