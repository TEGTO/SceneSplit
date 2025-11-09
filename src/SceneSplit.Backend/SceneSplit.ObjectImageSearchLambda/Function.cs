using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using SceneSplit.Configuration;
using SceneSplit.GrpcClientShared.Helpers;
using SceneSplit.GrpcClientShared.Interceptors;
using SceneSplit.ImageCompression.Sdk;
using SceneSplit.SceneAnalysisLambda.Sdk;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace SceneSplit.ObjectImageSearchLambda;

public sealed class Function
{
    private static JsonSerializerOptions jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Compression.CompressionClient compressionClient;
    private readonly ITransferUtility transferUtility;
    private readonly ObjectImageSearchLambdaOptions options;
    private readonly HttpClient httpClient;
    private readonly ILogger<Function> logger;

    public Function()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddLambdaLogger());
        logger = loggerFactory.CreateLogger<Function>();
        var errorLogger = loggerFactory.CreateLogger<GrpcErrorInterceptor>();
        var resilienceLogger = loggerFactory.CreateLogger<GrpcResilienceInterceptor>();

        transferUtility = new TransferUtility(new AmazonS3Client());
        options = ObjectImageSearchLambdaOptions.FromEnvironment();
        httpClient = new HttpClient();

        compressionClient = GrpcClientFactoryHelper.CreateGrpcClientWeb<Compression.CompressionClient>(
            uri: options.CompressionApiUrl,
            errorLogger: errorLogger,
            resilienceLogger: resilienceLogger,
            configureChannelOptions: channelOptions =>
            {
                channelOptions.MaxReceiveMessageSize = options.MaxImageSize;
                channelOptions.MaxSendMessageSize = options.MaxImageSize;
            });
    }

    public Function(
        ITransferUtility transferUtility,
        ObjectImageSearchLambdaOptions options,
        HttpClient httpClient,
        Compression.CompressionClient compressionClient,
        ILogger<Function> logger)
    {
        this.transferUtility = transferUtility;
        this.options = options;
        this.httpClient = httpClient;
        this.compressionClient = compressionClient;
        this.logger = logger;
    }

    public async Task Handler(SQSEvent sqsEvent)
    {
        foreach (var record in sqsEvent.Records)
        {
            Log.ProcessingMessage(logger, record.MessageId);

            SceneAnalysisResult? sceneAnalysis;
            try
            {
                sceneAnalysis = JsonSerializer.Deserialize<SceneAnalysisResult>(record.Body, jsonOptions);
                if (sceneAnalysis == null)
                {
                    Log.SkippingInvalidMessage(logger, record.MessageId);
                    throw new InvalidOperationException($"Invalid message format for ID {record.MessageId}");
                }
            }
            catch (Exception ex)
            {
                Log.FailedToProcessWithException(logger, ex, record.MessageId, ex.Message);
                throw;
            }

            try
            {
                foreach (var objectDescription in sceneAnalysis.ObjectDescriptions)
                {
                    Log.SearchingImages(logger, objectDescription);

                    var imageUrls = await SearchImagesAsync(objectDescription);
                    if (imageUrls.Count == 0)
                    {
                        Log.NoImagesFound(logger, objectDescription);
                        continue;
                    }

                    var tags = sceneAnalysis.WorkflowTags;
                    tags[WorkflowTags.DESCRIPTION] = objectDescription;

                    foreach (var imageUrl in imageUrls)
                    {
                        await UploadImageToS3Async(imageUrl, tags);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.FailedToProcess(logger, ex, record.MessageId);
                throw;
            }

            Log.ProcessedSuccessfully(logger, record.MessageId);
        }
    }

    private async Task<List<string>> SearchImagesAsync(string keyword)
    {
        var uri = $"{options.ImageSearchApiEndpoint}?query={Uri.EscapeDataString(keyword)}&per_page=1";
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.TryAddWithoutValidation("Authorization", $"Client-ID {options.ImageSearchApiKey}");
        request.Headers.TryAddWithoutValidation("Accept-Version", "v1");

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(stream);

        if (json.RootElement.ValueKind == JsonValueKind.Object &&
            json.RootElement.TryGetProperty("results", out var results))
        {
            return ExtractImageUrls(results);
        }

        if (json.RootElement.ValueKind == JsonValueKind.Array)
        {
            return ExtractImageUrls(json.RootElement);
        }

        return [];
    }

    private static List<string> ExtractImageUrls(JsonElement results)
    {
        return [.. results
            .EnumerateArray()
            .Select(img =>
            {
                string? url = null;

                if (img.TryGetProperty("urls", out var urls))
                {
                    if (urls.TryGetProperty("regular", out var regular))
                    {
                        url = regular.GetString();
                    }
                    else if (urls.TryGetProperty("full", out var full))
                    {
                        url = full.GetString();
                    }
                    else if (urls.TryGetProperty("small", out var small))
                    {
                        url = small.GetString();
                    }
                }

                return url;
            })
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Cast<string>()
        ];
    }

    private async Task UploadImageToS3Async(string imageUrl, Dictionary<string, string> workflowTags)
    {
        Log.DownloadingImage(logger, imageUrl);
        var imageBytes = await httpClient.GetByteArrayAsync(imageUrl);

        var compressionRequest = new CompressionRequest
        {
            FileName = $"{Guid.NewGuid()}.jpg",
            ImageData = Google.Protobuf.ByteString.CopyFrom(imageBytes),
            Quality = options.ImageQualityCompression,
            ResizeWidth = options.ResizeWidth,
            ResizeHeight = options.ResizeHeight,
            KeepAspectRatio = true
        };

        var response = await compressionClient.CompressImageAsync(compressionRequest);
        var compressedBytes = response.CompressedImage.ToByteArray();
        var fileName = $"{Guid.NewGuid()}.{response.Format}";

        using var stream = new MemoryStream(compressedBytes);
        var uploadRequest = new TransferUtilityUploadRequest
        {
            BucketName = options.BucketName,
            Key = fileName,
            InputStream = stream,
            ContentType = $"image/{response.Format}"
        };

        uploadRequest.TagSet = [.. workflowTags.Select(kvp => new Tag { Key = kvp.Key, Value = kvp.Value })];

        await transferUtility.UploadAsync(uploadRequest);
        Log.UploadedToBucket(logger, fileName, options.BucketName);
    }
}