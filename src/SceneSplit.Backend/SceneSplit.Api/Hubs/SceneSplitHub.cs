using Mapster;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using SceneSplit.Api.Commands.ProcessSceneImage;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Api.Queries.GetObjectImages;
using SceneSplit.Api.Sdk.Contracts;
using System.Collections.Concurrent;

namespace SceneSplit.Api.Hubs;

public class SceneSplitHub(IMediator mediator) : Hub<ISceneSplitHubClient>
{
    private static readonly ConcurrentDictionary<string, ICollection<ObjectImage>> userObjectImages = new();
    private static readonly ConcurrentDictionary<string, string> connectionToUser = new();

    public async Task StartListenToUserObjectImages(string userId)
    {
        connectionToUser[Context.ConnectionId] = userId;

        await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        var images = userObjectImages.GetOrAdd(userId, (uid) =>
        {
            var imgs = mediator.Send(new GetObjectImagesQuery(uid)).Result;
            return imgs;
        });

        await Clients.Caller.ReceiveImageLinks(images.Adapt<ICollection<ObjectImageResponse>>());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (!connectionToUser.TryRemove(Context.ConnectionId, out var userId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

        if (!connectionToUser.Values.Contains(userId))
        {
            userObjectImages.TryRemove(userId, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static async Task UpdateUserImagesAsync(
        string userId,
        IHubClients<ISceneSplitHubClient> clients,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!connectionToUser.Values.Contains(userId))
        {
            return;
        }

        var images = await mediator.Send(new GetObjectImagesQuery(userId), cancellationToken);
        userObjectImages[userId] = images;
        await clients.Group(userId).ReceiveImageLinks(images.Adapt<ICollection<ObjectImageResponse>>());
    }

    public async Task UploadSceneImageForUser(string userId, SendSceneImageRequest request)
    {
        await mediator.Send(new ProcessSceneImageCommand(userId, request.FileName, request.FileContent));
    }
}