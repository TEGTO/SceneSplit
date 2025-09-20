using SceneSplit.Api.Domain.Models;

namespace SceneSplit.Api.Sevices.ImagePersistent;

public interface IImagePersistentService
{
    public Task<ICollection<ObjectImage>> GetObjectImagesForUserAsync(string userId, CancellationToken cancellationToken = default);
    public Task UpdateObjectImagesForUserAsync(string userId, ICollection<ObjectImage> images, CancellationToken cancellationToken = default);
}