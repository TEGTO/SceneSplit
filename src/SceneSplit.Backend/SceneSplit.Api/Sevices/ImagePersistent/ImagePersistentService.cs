
using SceneSplit.Api.Domain.Models;

namespace SceneSplit.Api.Sevices.ImagePersistent;

public class ImagePersistentService : IImagePersistentService
{
    public Task<ICollection<ObjectImage>> GetObjectImagesForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ICollection<ObjectImage>>(
        [
            new()
            {
                ImageUrl = "https://images.unsplash.com/photo-1503023345310-bd7c1de61c7d",
                Description = "A beautiful sunrise over the mountains."
            },
            new()
            {
                ImageUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330",
                Description = "A portrait"
            },
        ]);
    }

    public Task UpdateObjectImagesForUserAsync(string userId, ICollection<ObjectImage> images, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}