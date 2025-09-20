using Mapster;
using Microsoft.AspNetCore.SignalR;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Api.Sdk.Contracts;
using SceneSplit.Api.Sevices.ImagePersistent;
using SceneSplit.Api.Sevices.SceneImageProcessor;
using System.Collections.Concurrent;

namespace SceneSplit.Api.Hubs;

public class SceneSplitHub(IImagePersistentService imagePersistent, ISceneImageProcessor sceneImageProcessor) : Hub<ISceneSplitHubClient>
{
    private static readonly ConcurrentDictionary<string, ICollection<ObjectImage>> userObjectImages = new();
    private static readonly ConcurrentDictionary<string, string> connectionToUser = new();

    public async Task StartListenToUserObjectImages(string userId)
    {
        connectionToUser[Context.ConnectionId] = userId;

        await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        var images = userObjectImages.GetOrAdd(userId, (uid) =>
        {
            var imgs = imagePersistent.GetObjectImagesForUserAsync(uid).Result;
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

    public async Task UpdateImagesForUser(string userId, ICollection<ObjectImage> images)
    {
        await imagePersistent.UpdateObjectImagesForUserAsync(userId, images);

        if (connectionToUser.Values.Contains(userId))
        {
            userObjectImages[userId] = images;
            await Clients.Group(userId).ReceiveImageLinks(images.Adapt<ICollection<ObjectImageResponse>>());
        }
    }

    public async Task UploadSceneImageForUser(string userId, SendSceneImageRequest request)
    {
        await sceneImageProcessor.ProcessSceneImageForUserAsync(userId, request.FileName, request.FileContent);
    }
}