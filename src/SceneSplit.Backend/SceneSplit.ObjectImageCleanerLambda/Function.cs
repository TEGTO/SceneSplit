using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using SceneSplit.Configuration;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SceneSplit.ObjectImageCleanerLambda;

public sealed class Function
{
    private readonly IAmazonS3 s3Client;
    private readonly ILogger<Function> logger;
    private readonly ObjectImageCleanerLambdaOptions options;

    public Function()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddLambdaLogger());
        logger = loggerFactory.CreateLogger<Function>();
        s3Client = new AmazonS3Client();
        options = ObjectImageCleanerLambdaOptions.FromEnvironment();
    }

    public Function(IAmazonS3 s3Client, ILogger<Function> logger, ObjectImageCleanerLambdaOptions options)
    {
        this.s3Client = s3Client;
        this.logger = logger;
        this.options = options;
    }

    public async Task Handler(S3Event s3Event)
    {
        foreach (var s3Entity in s3Event.Records.Select(r => r.S3))
        {
            var bucket = s3Entity.Bucket.Name;
            var key = s3Entity.Object.Key;

            Log.CleanerTriggered(logger, bucket, key);

            var (userId, workflowIdToKeep) = await GetEventContextAsync(bucket, key);
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(workflowIdToKeep))
            {
                Log.SkippingCleanupMissingTags(logger, bucket, key, userId, workflowIdToKeep);
                continue;
            }

            Log.StartingCleanup(logger, userId, workflowIdToKeep);

            var (toDelete, scanned) = await EnumerateKeysToDeleteAsync(bucket, key, userId, workflowIdToKeep);

            var deleted = await DeleteKeysAsync(bucket, toDelete);

            Log.CleanupCompleted(logger, userId, scanned, deleted, workflowIdToKeep);
        }
    }

    private async Task<(string? UserId, string? WorkflowId)> GetEventContextAsync(string bucket, string key)
    {
        var srcTagsResp = await s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
        {
            BucketName = bucket,
            Key = key
        });

        var srcTags = srcTagsResp.Tagging ?? [];
        var userId = srcTags.FirstOrDefault(t => t.Key == WorkflowTags.USER_ID_TAG)?.Value;
        var workflowIdToKeep = srcTags.FirstOrDefault(t => t.Key == WorkflowTags.WORKFLOW_ID)?.Value;

        return (userId, workflowIdToKeep);
    }

    private async Task<(List<string> KeysToDelete, int Scanned)> EnumerateKeysToDeleteAsync(
        string bucket,
        string triggeringKey,
        string userId,
        string workflowIdToKeep)
    {
        var keysToDelete = new List<string>();
        var scanned = 0;
        string? continuation = null;

        do
        {
            var listResp = await s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket,
                ContinuationToken = continuation
            });

            var pageKeys = listResp.S3Objects?.Select(o => o.Key).ToList() ?? [];
            if (pageKeys.Count == 0)
            {
                break;
            }

            scanned += pageKeys.Count;

            using var throttler = new SemaphoreSlim(options.MaxKeysSearchConcurrency);
            var tasks = pageKeys.Select(async k =>
            {
                if (string.Equals(k, triggeringKey, StringComparison.Ordinal))
                {
                    return;
                }

                await throttler.WaitAsync();
                try
                {
                    var tagResp = await s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
                    {
                        BucketName = bucket,
                        Key = k
                    });

                    var tags = tagResp.Tagging ?? [];
                    var uTag = tags.FirstOrDefault(t => t.Key == WorkflowTags.USER_ID_TAG)?.Value;
                    if (!string.Equals(uTag, userId, StringComparison.Ordinal))
                    {
                        return;
                    }

                    var wfTag = tags.FirstOrDefault(t => t.Key == WorkflowTags.WORKFLOW_ID)?.Value;
                    if (!string.Equals(wfTag, workflowIdToKeep, StringComparison.Ordinal))
                    {
                        lock (keysToDelete)
                        {
                            keysToDelete.Add(k);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.FailedToReadTags(logger, ex, bucket, k);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);

            continuation = listResp.IsTruncated == true ? listResp.NextContinuationToken : null;

        } while (continuation != null);

        return (keysToDelete, scanned);
    }

    private async Task<int> DeleteKeysAsync(string bucket, List<string> keys)
    {
        if (keys.Count == 0)
        {
            return 0;
        }

        var totalDeleted = 0;

        for (var i = 0; i < keys.Count; i += options.MaxDeleteBatch)
        {
            var slice = keys.Skip(i).Take(options.MaxDeleteBatch)
                .Select(k => new KeyVersion { Key = k })
                .ToList();

            var req = new DeleteObjectsRequest
            {
                BucketName = bucket,
                Objects = slice
            };

            var resp = await s3Client.DeleteObjectsAsync(req);
            totalDeleted += slice.Count;

            Log.DeletedBatch(logger, slice.Count, bucket);

            if (resp.DeleteErrors?.Count > 0)
            {
                foreach (var err in resp.DeleteErrors)
                {
                    Log.DeleteError(logger, err.Key, err.Code, err.Message);
                }
            }
        }

        return totalDeleted;
    }
}