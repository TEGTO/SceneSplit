using Amazon.CDK;
using Amazon.CDK.AWS.S3;

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

var objectImageBucket = awsResources.AddS3Bucket("scene-split-detected-object-images-bucket", new BucketProps
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

var imageCompressionApi = builder.AddProject<Projects.SceneSplit_ImageCompression_Api>("scenesplit-imagecompression-api");

builder.AddProject<Projects.SceneSplit_Api>("scenesplit-api")
    .WithReference(awsCdkConfig)
    .WithReference(sceneImageBucket)
    .WithReference(objectImageBucket)
    .WithReference(imageCompressionApi);

await builder.Build().RunAsync();