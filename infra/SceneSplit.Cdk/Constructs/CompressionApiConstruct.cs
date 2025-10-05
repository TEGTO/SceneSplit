using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.ServiceDiscovery;
using Constructs;
using SceneSplit.Configuration;
using ApplicationLoadBalancerProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancerProps;

namespace SceneSplit.Cdk.Constructs;

public class CompressionApiConstruct : Construct
{
    public ApplicationLoadBalancer Service { get; }

    public CompressionApiConstruct(Construct scope, string id, Vpc vpc, INamespace namespaceRef)
        : base(scope, id)
    {
        var secGroup = new SecurityGroup(this, "CompressionApiLambdaGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        var lambdaFunction = new Function(this, "CompressionApiLambda", new FunctionProps
        {
            FunctionName = "scene-split-compression-api",
            Runtime = Runtime.DOTNET_8,
            MemorySize = 128,
            Timeout = Duration.Seconds(30),
            Handler = "SceneSplit.ImageCompression.Api",
            Vpc = vpc,
            SecurityGroups = [secGroup],
            LogRetention = RetentionDays.THREE_DAYS,
            Environment = new Dictionary<string, string>
            {
                { "ASPNETCORE_ENVIRONMENT", "Production" },
                { ImageCompressionApiConfigurationKeys.ALLOWED_IMAGE_TYPES, ".jpg,.jpeg,.png" },
                { ImageCompressionApiConfigurationKeys.MAX_IMAGE_SIZE, (10 * 1024 * 1024).ToString() },
            },
            Code = Code.FromAsset("./src/SceneSplit.Backend/publish/SceneSplit.ImageCompression.Api.zip")
        });

        Service = new ApplicationLoadBalancer(this, "InternalALB", new ApplicationLoadBalancerProps
        {
            Vpc = vpc,
            InternetFacing = false,
            SecurityGroup = secGroup,
        });

        var listener = Service.AddListener("Listener", new BaseApplicationListenerProps
        {
            Port = 80
        });

        listener.AddTargets("LambdaTarget", new AddApplicationTargetsProps
        {
            Targets = [new LambdaTarget(lambdaFunction)]
        });
    }
}