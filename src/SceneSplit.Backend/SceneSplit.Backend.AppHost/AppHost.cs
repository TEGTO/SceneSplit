#pragma warning disable CA2252 // Opt-in to preview features

using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SQS;
using Amazon.Lambda;
using Aspire.Hosting.AWS.Lambda;

var builder = DistributedApplication.CreateBuilder(args);

var awsCdkConfig = builder.AddAWSSDKConfig();

var awsResources = builder.AddAWSCDKStack("SceneSplitStackDev")
    .WithReference(awsCdkConfig);

var sceneImageBucket = awsResources.AddS3Bucket("scene-split-scene-images-bucket", new BucketProps
{
    BucketName = "scene-split-scene-images",
    RemovalPolicy = RemovalPolicy.DESTROY,
    PublicReadAccess = true,
    BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
    {
        BlockPublicAcls = false,
        IgnorePublicAcls = false,
        BlockPublicPolicy = false,
        RestrictPublicBuckets = false
    }),
    Encryption = BucketEncryption.S3_MANAGED
});

var detectedObjectImageBucket = awsResources.AddS3Bucket("scene-split-detected-object-images-bucket", new BucketProps
{
    BucketName = "scene-split-detected-object-images",
    RemovalPolicy = RemovalPolicy.DESTROY,
    PublicReadAccess = true,
    BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
    {
        BlockPublicAcls = false,
        IgnorePublicAcls = false,
        BlockPublicPolicy = false,
        RestrictPublicBuckets = false
    }),
    Encryption = BucketEncryption.S3_MANAGED
});

var detectedObjectsQueue = awsResources.AddSQSQueue("scene-split-detected-objects-queue", new QueueProps
{
    QueueName = "scene-split-detected-objects",
    VisibilityTimeout = Duration.Seconds(60),
    RetentionPeriod = Duration.Days(1),
    RemovalPolicy = RemovalPolicy.DESTROY
});

var imageCompressionApi = builder.AddProject<Projects.SceneSplit_ImageCompression_Api>("scenesplit-imagecompression-api")
    .WithEnvironment("MAX_IMAGE_SIZE", "10485760")
    .WithEnvironment("ALLOWED_IMAGE_TYPES", ".jpg,.jpeg,.png");

builder.AddProject<Projects.SceneSplit_Api>("scenesplit-api")
    .WithReference(awsCdkConfig)
    .WithReference(sceneImageBucket)
    .WithReference(detectedObjectImageBucket)
    .WithReference(imageCompressionApi)
    .WithEnvironment("ALLOWED_CORS_ORIGINS", "*")
    .WithEnvironment("COMPRESSION_API_URL", "http://scenesplit-imagecompression-api")
    .WithEnvironment("ALLOWED_IMAGE_TYPES", ".jpg,.jpeg,.png")
    .WithEnvironment("SCENE_IMAGE_BUCKET", "scene-split-scene-images")
    .WithEnvironment("OBJECT_IMAGE_BUCKET", "scene-split-detected-object-images")
    .WithEnvironment("OBJECT_IMAGE_POLL_INTERVAL_SECONDS", "10")
    .WithEnvironment("MAX_IMAGE_SIZE", "10485760")
    .WithEnvironment("RESIZE_WIDTH", "1024")
    .WithEnvironment("RESIZE_HEIGHT", "1024")
    .WithEnvironment("IMAGE_QUALITY_COMPRESSION", "75");

builder.AddAWSLambdaFunction<Projects.SceneSplit_SceneAnalysisLambda>(
        "scene-split-scene-analysis-lambda",
        lambdaHandler: "SceneSplit.SceneAnalysisLambda::SceneSplit.SceneAnalysisLambda.Function::Handler",
        options: new LambdaFunctionOptions
        {
            ApplicationLogLevel = ApplicationLogLevel.DEBUG,
            LogFormat = LogFormat.JSON,
        })
    .WithReference(awsCdkConfig)
    .WithReference(detectedObjectsQueue)
    .WithReference(sceneImageBucket)
    .WithEnvironment("SQS_QUEUE_URL", "scene-split-detected-objects")
    .WithEnvironment("MAX_ITEMS", "10")
    .WithEnvironment("BEDROCK_MODEL_ID", "eu.amazon.nova-lite-v1:0");


builder.AddAWSLambdaFunction<Projects.SceneSplit_ObjectImageSearchLambda>(
        "object-image-search-lambda",
        lambdaHandler: "SceneSplit.ObjectImageSearchLambda::SceneSplit.ObjectImageSearchLambda.Function::Handler",
        options: new LambdaFunctionOptions
        {
            ApplicationLogLevel = ApplicationLogLevel.DEBUG,
            LogFormat = LogFormat.JSON,
        })
    .WithReference(awsCdkConfig)
    .WithReference(detectedObjectImageBucket)
    .WithReference(imageCompressionApi)
    .WithEnvironment("BUCKET_NAME", "scene-split-detected-objects")
    .WithEnvironment("IMAGE_SEARCH_API_KEY", "")
    .WithEnvironment("IMAGE_SEARCH_API_ENDPOINT", "https://api.unsplash.com/search/photos")
    .WithEnvironment("COMPRESSION_API_URL", "http://scenesplit-imagecompression-api")
    .WithEnvironment("MAX_IMAGE_SIZE", "10485760")
    .WithEnvironment("RESIZE_WIDTH", "1024")
    .WithEnvironment("RESIZE_HEIGHT", "1024")
    .WithEnvironment("IMAGE_QUALITY_COMPRESSION", "75");

builder.AddAWSLambdaFunction<Projects.SceneSplit_ObjectImageCleanerLambda>(
        "object-image-cleaner-lambda",
        lambdaHandler: "SceneSplit.ObjectImageCleanerLambda::SceneSplit.ObjectImageCleanerLambda.Function::Handler",
        options: new LambdaFunctionOptions
        {
            ApplicationLogLevel = ApplicationLogLevel.DEBUG,
            LogFormat = LogFormat.JSON,
        })
    .WithReference(awsCdkConfig)
    .WithReference(detectedObjectImageBucket);

await builder.Build().RunAsync();