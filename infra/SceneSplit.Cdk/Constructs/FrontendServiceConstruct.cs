using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;
using SceneSplit.Cdk.Helpers;

namespace SceneSplit.Cdk.Constructs;

public class FrontendServiceConstruct : Construct
{
    public ApplicationLoadBalancedFargateService Service { get; }

    public FrontendServiceConstruct(Construct scope, string id, Cluster cluster, string apiHubEndpoint)
        : base(scope, id)
    {
        Service = new ApplicationLoadBalancedFargateService(this, "FrontendService", new ApplicationLoadBalancedFargateServiceProps
        {
            Cluster = cluster,
            DesiredCount = 1,
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                Image = ContainerImage.FromAsset("src/SceneSplit.Frontend", new AssetImageProps
                {
                    File = "Dockerfile",
                    BuildArgs = new Dictionary<string, string>
                    {
                        { "ENV", "production" }
                    },
                }),
                ContainerPort = 80,
                Environment = new Dictionary<string, string>
                {
                    { "HUB_URL", apiHubEndpoint }
                },
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = "frontendServiceLogs"
                }),
            },
            MemoryLimitMiB = 1024,
            Cpu = 512,
            ServiceName = "frontend",
            PublicLoadBalancer = true,
            AssignPublicIp = true,
            TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            HealthCheck = TaskHelpers.AddHealthCheckForTask("80/health"),
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200
        });

        Service.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
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