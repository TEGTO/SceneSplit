using Amazon.CDK;

namespace SceneSplit.Cdk;

static class Program
{
    public static void Main()
    {
        var app = new App();

        _ = new SceneSplitStack(app, nameof(SceneSplitStack), new SceneSplitStackProps());
        app.Synth();
    }
}