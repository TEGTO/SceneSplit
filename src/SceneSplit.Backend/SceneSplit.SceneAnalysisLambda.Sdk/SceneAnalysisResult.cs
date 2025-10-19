namespace SceneSplit.SceneAnalysisLambda.Sdk;

public record SceneAnalysisResult
{
    public Dictionary<string, string> WorkflowTags { get; init; } = [];
    public List<string> Items { get; init; } = [];
}