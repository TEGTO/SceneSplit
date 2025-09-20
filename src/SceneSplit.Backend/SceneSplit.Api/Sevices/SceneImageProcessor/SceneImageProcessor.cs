
using Microsoft.AspNetCore.SignalR;
using SceneSplit.Configuration;

namespace SceneSplit.Api.Sevices.SceneImageProcessor;

public class SceneImageProcessor : ISceneImageProcessor
{
    private readonly string[] allowedImageTypes;
    private readonly int maxFileSizeInBytes;

    public SceneImageProcessor(IConfiguration configuration)
    {
        var allowedImageTypesConfig = configuration[ApiConfigurationKeys.ALLOWED_IMAGE_TYPES] ?? ".jpg,.jpeg,.png";
        var maxFileSizeInBytesConfig = configuration[ApiConfigurationKeys.MAX_IMAGE_SIZE] ?? (10 * 1024 * 1024).ToString();

        this.allowedImageTypes = allowedImageTypesConfig.Split(",");
        this.maxFileSizeInBytes = int.Parse(maxFileSizeInBytesConfig);
    }

    public async Task ProcessSceneImageForUserAsync(
        string userId, string fileName, byte[] fileContent, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!allowedImageTypes.Contains(extension))
        {
            throw new HubException("Only PNG and JPG files are allowed.");
        }

        if (fileContent.Length > maxFileSizeInBytes)
        {
            throw new HubException("File size exceeds 5MB limit.");
        }
    }
}