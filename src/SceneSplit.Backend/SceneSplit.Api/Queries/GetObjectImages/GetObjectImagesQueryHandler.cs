using MediatR;
using SceneSplit.Api.Domain.Models;

namespace SceneSplit.Api.Queries.GetObjectImages;

public class GetObjectImagesQueryHandler : IRequestHandler<GetObjectImagesQuery, ICollection<ObjectImage>>
{
    public Task<ICollection<ObjectImage>> Handle(GetObjectImagesQuery request, CancellationToken cancellationToken)
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
            }
        ]);
    }
}