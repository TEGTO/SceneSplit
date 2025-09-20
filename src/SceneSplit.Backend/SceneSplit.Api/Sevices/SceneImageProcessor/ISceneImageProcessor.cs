namespace SceneSplit.Api.Sevices.SceneImageProcessor;

public interface ISceneImageProcessor
{
    public Task ProcessSceneImageForUserAsync(string userId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default);
}
