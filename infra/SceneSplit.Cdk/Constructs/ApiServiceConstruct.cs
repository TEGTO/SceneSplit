using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Constructs;
using SceneSplit.Cdk.Helpers;
using SceneSplit.Configuration;

namespace SceneSplit.Cdk.Constructs;

public class ApiServiceConstruct : Construct
{
    public ApplicationLoadBalancedFargateService Service { get; }

    public ApiServiceConstruct(Construct scope, string id, Cluster cluster, Vpc vpc)
        : base(scope, id)
    {
        var apiSecGroup = new SecurityGroup(this, "ApiServiceSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        Service = new ApplicationLoadBalancedFargateService(this, "ApiService", new ApplicationLoadBalancedFargateServiceProps
        {
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
                    { ApiConfigurationKeys.ALLOWED_IMAGE_TYPES, ".jpg,.jpeg,.png" }
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
            ServiceName = "api",
            HealthCheck = TaskHelpers.AddHealthCheckForTask("8080/health"),
            MinHealthyPercent = 100,
            MaxHealthyPercent = 200
        });

        Service.TargetGroup.EnableCookieStickiness(Duration.Minutes(30));
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