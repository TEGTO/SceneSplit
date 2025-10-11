using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;
using SceneSplit.Cdk.Helpers;
using SceneSplit.Configuration;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;

namespace SceneSplit.Cdk.Constructs;

public class ApiServiceConstruct : Construct
{
    public ApplicationLoadBalancedFargateService FargateService { get; }

    public ApiServiceConstruct(Construct scope, string id, Cluster cluster, Vpc vpc, string compressionApiUrl, ISecurityGroup compressionApiSecGroup, string sceneImageBucket)
        : base(scope, id)
    {
        var apiSecGroup = new SecurityGroup(this, "ApiServiceSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        compressionApiSecGroup.AddIngressRule(apiSecGroup, Port.Tcp(443), "Allow api to access compression api");

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
                    { ApiConfigurationKeys.SCENE_IMAGE_BUCKET, sceneImageBucket },
                },
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = "apiServiceLogs"
                }),
            },
            SecurityGroups = [apiSecGroup],
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
    }
}