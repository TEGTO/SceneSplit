namespace SceneSplit.Api.Sdk.Contracts;

public record SendSceneImageRequest
{
    public required string FileName { get; init; }
    public required byte[] FileContent { get; init; }
}