using Amazon.BedrockRuntime;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using SceneSplit.Configuration;
using SceneSplit.SceneAnalysisLambda.Sdk;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SceneSplit.SceneAnalysisLambda;

public sealed class Function
{
    private readonly IAmazonS3 s3Client;
    private readonly IAmazonSQS sqsClient;
    private readonly IChatClient aiClient;
    private readonly SceneAnalysisLambdaOptions options;
    private readonly ILogger<Function> logger;

    public Function()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddLambdaLogger());
        logger = loggerFactory.CreateLogger<Function>();

        s3Client = new AmazonS3Client();
        sqsClient = new AmazonSQSClient();
        options = SceneAnalysisLambdaOptions.FromEnvironment();
        aiClient = new AmazonBedrockRuntimeClient().AsIChatClient(options.BedrockModelId);
    }

    public Function(
        IAmazonS3 s3Client,
        IAmazonSQS sqsClient,
        IChatClient aiClient,
        SceneAnalysisLambdaOptions options,
        ILogger<Function> logger)
    {
        this.s3Client = s3Client;
        this.sqsClient = sqsClient;
        this.options = options;
        this.aiClient = aiClient;
        this.logger = logger;
    }

    public async Task Handler(S3Event s3Event)
    {
        foreach (var s3Entity in s3Event.Records.Select(r => r.S3))
        {
            var bucket = s3Entity.Bucket.Name;
            var key = s3Entity.Object.Key;

            Log.ProcessingS3Object(logger, bucket, key);

            try
            {
                var workflowTags = await GetWorkflowTagsAsync(bucket, key);

                var (imageBytes, mimeType) = await DownloadImageAsync(bucket, key);
                var objectDescriptions = await AnalyzeImageAsync(imageBytes, mimeType);

                if (objectDescriptions.Count == 0)
                {
                    Log.NoItemsDetected(logger, key);
                    continue;
                }

                var message = new SceneAnalysisResult
                {
                    WorkflowTags = workflowTags,
                    ObjectDescriptions = objectDescriptions
                };

                await PublishResultAsync(message);
            }
            catch (Exception ex)
            {
                Log.FailedToProcessImage(logger, ex, bucket, key);
                throw;
            }
        }
    }

    private async Task<Dictionary<string, string>> GetWorkflowTagsAsync(string bucket, string key)
    {
        var response = await s3Client.GetObjectTaggingAsync(new GetObjectTaggingRequest
        {
            BucketName = bucket,
            Key = key
        });

        var tagsDict = (response.Tagging ?? [])
            .Where(t => WorkflowTags.ALL_TAGS.Contains(t.Key))
            .ToDictionary(t => t.Key, t => t.Value);

        foreach (var requiredTag in
            WorkflowTags.ALL_TAGS.Where(requiredTag => !tagsDict.ContainsKey(requiredTag)))
        {
            tagsDict[requiredTag] = WorkflowTags.UNKNOWN;
        }

        return tagsDict;
    }

    private async Task<(byte[] Bytes, string MimeType)> DownloadImageAsync(string bucket, string key)
    {
        using var response = await s3Client.GetObjectAsync(bucket, key);

        var rawMime = response.Headers.ContentType;

        var normalized = MimeHelper.NormalizeMime(rawMime, key);

        await using var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);

        return (ms.ToArray(), normalized);
    }

    private async Task<List<string>> AnalyzeImageAsync(byte[] imageBytes, string mimeType)
    {
        var message = new ChatMessage(ChatRole.User, $$"""
            Analyze the image and return up to {{options.MaxItems}} items as a JSON array of strings.
            Use the following format:
            {
                "items": ["item1", "item2", "item3"]
            }
            """);

        message.Contents.Add(new DataContent(imageBytes, mimeType));

        var response = await aiClient.GetResponseAsync<SceneAnalysisAIResponse>(message);
        return response.Result.Items ?? [];
    }

    private async Task PublishResultAsync(SceneAnalysisResult message)
    {
        var body = JsonSerializer.Serialize(message);
        await sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = options.SqsQueueUrl,
            MessageBody = body
        });

        Log.PublishedAnalysisResult(logger, body);
    }
}