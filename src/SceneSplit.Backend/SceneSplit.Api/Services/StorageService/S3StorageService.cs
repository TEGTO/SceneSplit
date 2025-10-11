using Amazon.S3.Transfer;
using SceneSplit.Configuration;

namespace SceneSplit.Api.Services.StorageService;

public class S3StorageService : IStorageService
{
    private const string USER_ID_TAG = "UserId";

    private readonly ITransferUtility transferUtility;
    private readonly string bucketName;

    public S3StorageService(ITransferUtility transferUtility, IConfiguration configuration)
    {
        this.transferUtility = transferUtility ?? throw new ArgumentNullException(nameof(transferUtility));
        bucketName = configuration[ApiConfigurationKeys.SCENE_IMAGE_BUCKET]
            ?? throw new InvalidOperationException("Missing S3 bucket configuration.");
    }

    public async Task UploadSceneImageAsync(
        string fileName,
        byte[] content,
        string contentType,
        string userId,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(content);

        var uploadRequest = new TransferUtilityUploadRequest
        {
            InputStream = stream,
            BucketName = bucketName,
            Key = fileName,
            ContentType = contentType,
            TagSet =
            [
                new () { Key = USER_ID_TAG, Value = userId }
            ]
        };

        await transferUtility.UploadAsync(uploadRequest, cancellationToken);
    }
}