using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.S3;
using Constructs;
using SceneSplit.Cdk.Helpers;
using SceneSplit.Configuration;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;

namespace SceneSplit.Cdk.Constructs;

public class ApiServiceConstruct : Construct
{
    public ApplicationLoadBalancedFargateService FargateService { get; }

    public ApiServiceConstruct(
        Construct scope,
        string id,
        Cluster cluster,
        Vpc vpc,
        string compressionApiUrl,
        Bucket sceneImageBucket,
        Bucket objectImageBucket)
        : base(scope, id)
    {
        var secGroup = new SecurityGroup(this, "ApiServiceSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        secGroup.AddIngressRule(Peer.Ipv4(vpc.VpcCidrBlock), Port.Tcp(8080), "Allow all traffic within VPC");

        FargateService = new ApplicationLoadBalancedFargateService(this, "ApiService", new ApplicationLoadBalancedFargateServiceProps
        {
            ServiceName = "scene-split-api",
            Cluster = cluster,
            DesiredCount = 1,
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                Image = ContainerImage.FromAsset(".", new AssetImageProps
                {
                    File = "src/SceneSplit.Backend/ApiDockerfile"
                }),
                ContainerPort = 8080,
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { ApiConfigurationKeys.ALLOWED_CORS_ORIGINS, "*" },
                    { ApiConfigurationKeys.MAX_IMAGE_SIZE, (10 * 1024 * 1024).ToString() },
                    { ApiConfigurationKeys.ALLOWED_IMAGE_TYPES, ".jpg,.jpeg,.png" },
                    { ApiConfigurationKeys.COMPRESSION_API_URL, compressionApiUrl },
                    { ApiConfigurationKeys.SCENE_IMAGE_BUCKET, sceneImageBucket.BucketName },
                    { ApiConfigurationKeys.OBJECT_IMAGE_BUCKET, objectImageBucket.BucketName },
                    { ApiConfigurationKeys.OBJECT_IMAGE_POLL_INTERVAL_SECONDS, "10" },
                },
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = "apiServiceLogs"
                }),
            },
            SecurityGroups = [secGroup],
            MemoryLimitMiB = 512,
            Cpu = 256,
            PublicLoadBalancer = false,
            HealthCheck = TaskHelpers.AddHealthCheckForTask("8080/health"),
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200
        });

        FargateService.TargetGroup.EnableCookieStickiness(Duration.Minutes(30));
        FargateService.TargetGroup.ConfigureHealthCheck(new HealthCheck
        {
            Path = "/health",
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200-299",
            HealthyThresholdCount = 2,
            UnhealthyThresholdCount = 3
        });

        sceneImageBucket.GrantReadWrite(FargateService.TaskDefinition.TaskRole);
        objectImageBucket.GrantRead(FargateService.TaskDefinition.TaskRole);
    }
}