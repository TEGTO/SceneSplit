using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.ServiceDiscovery;
using Amazon.CDK.AWS.SQS;
using Constructs;
using SceneSplit.Cdk.Constructs;

namespace SceneSplit.Cdk;

public class SceneSplitStack : Stack
{
    internal SceneSplitStack(Construct scope, string id, IStackProps props = null!) : base(scope, id, props)
    {
        var vpc = new Vpc(this, "Vpc", new VpcProps { MaxAzs = 2 });

        var cluster = new Cluster(this, "Cluster", new ClusterProps
        {
            Vpc = vpc,
            ClusterName = "scene-split-cluster",
            DefaultCloudMapNamespace = new CloudMapNamespaceOptions
            {
                Name = "scene-split",
                Type = NamespaceType.DNS_PRIVATE,
                Vpc = vpc
            },
        });

        var sceneImageBucket = new Bucket(this, "SceneImageBucket", new BucketProps
        {
            BucketName = "scene-split-scene-images",
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Versioned = false,
            Encryption = BucketEncryption.S3_MANAGED
        });

        var sceneImageDetectedObjectsQueue = new Queue(this, "SceneSplitSceneImagDetectedObjectsQueue", new QueueProps
        {
            QueueName = "scene-split-detected-objects",
            VisibilityTimeout = Duration.Seconds(60),
            RetentionPeriod = Duration.Days(1),
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var detectedObjectImageBucket = new Bucket(this, "DetectedObjectImageBucket", new BucketProps
        {
            BucketName = "scene-split-detected-object-images",
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            BlockPublicAccess = new BlockPublicAccess(new BlockPublicAccessOptions
            {
                BlockPublicAcls = false,
                BlockPublicPolicy = false,
                IgnorePublicAcls = false,
                RestrictPublicBuckets = false
            }),
            PublicReadAccess = false,
            Versioned = false,
            Encryption = BucketEncryption.S3_MANAGED
        });

        detectedObjectImageBucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Sid = "PublicReadGetObject",
            Actions = ["s3:GetObject"],
            Principals = [new AnyPrincipal()],
            Resources = [$"{detectedObjectImageBucket.BucketArn}/*"]
        }));

        var compressionApiService = new CompressionApiServiceConstruct(this, "CompressionApiServiceConstruct", cluster, vpc);

        var compressionApiUrl = $"http://{compressionApiService.FargateService.LoadBalancer.LoadBalancerDnsName}";
        var apiService = new ApiServiceConstruct(
            this,
            "ApiServiceConstruct",
            cluster,
            vpc,
            compressionApiUrl,
            sceneImageBucket,
            detectedObjectImageBucket
        );

        _ = new ObjectImageSearchLambdaConstruct(
            this,
            "ObjectImageSearchLambdaConstruct",
            vpc,
            sceneImageDetectedObjectsQueue,
            detectedObjectImageBucket,
            compressionApiUrl
        );

        _ = new SceneAnalysisLambdaConstruct(
            this,
            "SceneAnalysisLambdaConstruct",
            vpc,
            sceneImageBucket,
            sceneImageDetectedObjectsQueue
       );

        var apiEndpoint = $"http://{apiService.FargateService.LoadBalancer.LoadBalancerDnsName}";
        _ = new FrontendServiceConstruct(this, "FrontendServiceConstruct", cluster, vpc, apiEndpoint);
    }
}