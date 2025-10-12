using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;
using SceneSplit.Cdk.Helpers;
using SceneSplit.Configuration;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;

namespace SceneSplit.Cdk.Constructs;

public class CompressionApiServiceConstruct : Construct
{
    public ApplicationLoadBalancedFargateService FargateService { get; }

    public CompressionApiServiceConstruct(Construct scope, string id, Cluster cluster, Vpc vpc)
        : base(scope, id)
    {
        var secGroup = new SecurityGroup(this, "CompressionApiServiceSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        secGroup.AddIngressRule(Peer.Ipv4(vpc.VpcCidrBlock), Port.Tcp(80), "Allow all traffic within VPC");

        FargateService = new ApplicationLoadBalancedFargateService(this, "CompressionApiService", new ApplicationLoadBalancedFargateServiceProps
        {
            ServiceName = "scene-split-compression-api",
            Cluster = cluster,
            DesiredCount = 1,
            MemoryLimitMiB = 512,
            Cpu = 256,
            PublicLoadBalancer = false,
            SecurityGroups = [secGroup],
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                Image = ContainerImage.FromAsset(".", new AssetImageProps
                {
                    File = "src/SceneSplit.Backend/CompressionApiDockerfile"
                }),
                ContainerPort = 8080,
                Environment = new Dictionary<string, string>
                {
                    { "ASPNETCORE_ENVIRONMENT", "Production" },
                    { ImageCompressionApiConfigurationKeys.ALLOWED_IMAGE_TYPES, ".jpg,.jpeg,.png" },
                    { ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE, (10 * 1024 * 1024).ToString() }
                },
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = "compressionApiLogs"
                }),
            },
            HealthCheck = TaskHelpers.AddHealthCheckForTask("8080/health"),
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200
        });

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