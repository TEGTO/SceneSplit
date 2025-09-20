using SceneSplit.Api.Sdk.Contracts;

namespace SceneSplit.Api.Hubs;

public interface ISceneSplitHubClient
{
    public Task ReceiveImageLinks(ICollection<ObjectImageResponse> links);
}