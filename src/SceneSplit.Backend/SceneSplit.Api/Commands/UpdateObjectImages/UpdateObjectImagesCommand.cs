using MediatR;
using SceneSplit.Api.Domain.Models;

namespace SceneSplit.Api.Commands.UpdateObjectImages;

public record UpdateObjectImagesCommand(string UserId, ICollection<ObjectImage> Images) : IRequest;
