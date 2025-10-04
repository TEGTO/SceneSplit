
namespace SceneSplit.Api.Services.StorageService;

public interface IStorageService
{
    Task UploadSceneImageAsync(string fileName, byte[] content, string contentType, string userId, CancellationToken cancellationToken);
}