using MediatR;
using SceneSplit.Api.Domain.Models;

namespace SceneSplit.Api.Queries.GetObjectImages;

public record GetObjectImagesQuery(string UserId) : IRequest<ICollection<ObjectImage>>;