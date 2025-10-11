using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.SSM;
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

        var certificateArn = StringParameter.ValueForStringParameter(this, "/scene-split/cert-arn");
        var certificate = Certificate.FromCertificateArn(this, "CompressionApiCertificate", certificateArn);

        FargateService = new ApplicationLoadBalancedFargateService(this, "CompressionApiService", new ApplicationLoadBalancedFargateServiceProps
        {
            ServiceName = "scene-split-compression-api",
            Cluster = cluster,
            DesiredCount = 1,
            MemoryLimitMiB = 512,
            Cpu = 256,
            PublicLoadBalancer = false,
            SecurityGroups = [secGroup],
            ListenerPort = 443,
            Certificate = certificate,
            Protocol = ApplicationProtocol.HTTPS,
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
                    { "Kestrel__EndpointDefaults__Protocols", "Http1" },
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