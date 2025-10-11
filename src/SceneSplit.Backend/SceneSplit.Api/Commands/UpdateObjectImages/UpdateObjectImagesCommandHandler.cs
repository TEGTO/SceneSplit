using MediatR;

namespace SceneSplit.Api.Commands.UpdateObjectImages;

public class UpdateObjectImagesCommandHandler : IRequestHandler<UpdateObjectImagesCommand>
{
    public Task Handle(UpdateObjectImagesCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}