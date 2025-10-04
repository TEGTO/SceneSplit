using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using SceneSplit.Configuration;

namespace SceneSplit.Api.Services.StorageService;

public class S3StorageService : IStorageService
{
    private const string USER_ID_TAG = "UserId";

    private readonly IAmazonS3 s3Client;
    private readonly string bucketName;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        this.s3Client = s3Client;
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
            ContentType = contentType
        };

        uploadRequest.TagSet.Add(new Tag { Key = USER_ID_TAG, Value = userId });

        var transferUtility = new TransferUtility(s3Client);
        await transferUtility.UploadAsync(uploadRequest, cancellationToken);
    }
}