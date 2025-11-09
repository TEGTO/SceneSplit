using Amazon.S3;
using Amazon.S3.Model;
using MediatR;
using SceneSplit.Api.Domain.Models;
using SceneSplit.Configuration;

namespace SceneSplit.Api.Queries.GetObjectImages;

public class GetObjectImagesQueryHandler : IRequestHandler<GetObjectImagesQuery, ICollection<ObjectImage>>
{
    private readonly IAmazonS3 s3;
    private readonly string bucketName;
    private readonly ILogger<GetObjectImagesQueryHandler> logger;

    public GetObjectImagesQueryHandler(
        IAmazonS3 s3,
        IConfiguration configuration,
        ILogger<GetObjectImagesQueryHandler> logger)
    {
        this.s3 = s3;
        this.logger = logger;
        bucketName = configuration[ApiConfigurationKeys.OBJECT_IMAGE_BUCKET]
            ?? throw new InvalidOperationException($"{ApiConfigurationKeys.OBJECT_IMAGE_BUCKET} missing");
    }

    public async Task<ICollection<ObjectImage>> Handle(GetObjectImagesQuery request, CancellationToken cancellationToken)
    {
        var userId = request.UserId;
        Log.FetchingUserImages(logger, userId, bucketName);

        var candidates = new List<(string Key, DateTime? LastModified, string WorkflowId, string? Description)>();

        string? continuation = null;
        do
        {
            var listReq = new ListObjectsV2Request
            {
                BucketName = bucketName,
                ContinuationToken = continuation
            };

            var listResp = await s3.ListObjectsV2Async(listReq, cancellationToken);

            if (listResp.S3Objects == null || listResp.S3Objects.Count == 0)
            {
                break;
            }

            foreach (var obj in listResp.S3Objects)
            {
                var tagging = await s3.GetObjectTaggingAsync(new GetObjectTaggingRequest
                {
                    BucketName = bucketName,
                    Key = obj.Key
                }, cancellationToken);

                if (tagging.Tagging == null)
                {
                    continue;
                }

                var userTag = tagging.Tagging.FirstOrDefault(t => t.Key == WorkflowTags.USER_ID_TAG);
                if (userTag == null || !string.Equals(userTag.Value, userId, StringComparison.Ordinal))
                {
                    continue;
                }

                var workflowTag = tagging.Tagging.FirstOrDefault(t => t.Key == WorkflowTags.WORKFLOW_ID);
                var workflowId = workflowTag?.Value ?? WorkflowTags.UNKNOWN;

                var descriptionTag = tagging.Tagging.FirstOrDefault(t => t.Key == WorkflowTags.DESCRIPTION);
                var description = descriptionTag?.Value;

                candidates.Add((obj.Key, obj.LastModified?.ToUniversalTime(), workflowId, description));
            }

            continuation = listResp.IsTruncated == true ? listResp.NextContinuationToken : null;

        } while (continuation != null);

        if (candidates.Count == 0)
        {
            Log.NoImagesFoundForUser(logger, userId);
            return [];
        }

        var latestWorkflowId = candidates
            .GroupBy(c => c.WorkflowId)
            .Select(g => new { WorkflowId = g.Key, Latest = g.Max(x => x.LastModified) })
            .OrderByDescending(x => x.Latest)
            .First().WorkflowId;

        Log.LatestWorkflowResolved(logger, userId, latestWorkflowId);

        var final = candidates
            .Where(c => c.WorkflowId == latestWorkflowId)
            .Select(c => new ObjectImage
            {
                ImageUrl = BuildPublicUrl(bucketName, c.Key),
                Description = c.Description ?? Path.GetFileNameWithoutExtension(c.Key)
            })
            .ToList();

        Log.ReturningImagesForWorkflow(logger, final.Count, userId, latestWorkflowId);

        return final;
    }

    private static string BuildPublicUrl(string bucket, string key)
    {
        return $"https://{bucket}.s3.amazonaws.com/{Uri.EscapeDataString(key)}";
    }
}