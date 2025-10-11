using MediatR;

namespace SceneSplit.Api.Commands.ProcessSceneImage;

public record ProcessSceneImageCommand(string UserId, string FileName, byte[] FileContent) : IRequest;