namespace SceneSplit.Api.Sdk.Contracts;

public record ObjectImageResponse
{
    public required string ImageUrl { get; init; }
    public required string Description { get; init; }
}