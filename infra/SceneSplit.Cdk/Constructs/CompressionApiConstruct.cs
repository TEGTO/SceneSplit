using Amazon.CDK;
using Amazon.CDK.AWS.CertificateManager;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SSM;
using Constructs;
using SceneSplit.Configuration;
using ApplicationLoadBalancerProps = Amazon.CDK.AWS.ElasticLoadBalancingV2.ApplicationLoadBalancerProps;

namespace SceneSplit.Cdk.Constructs;

public class CompressionApiConstruct : Construct
{
    public ApplicationLoadBalancer Service { get; }

    public CompressionApiConstruct(Construct scope, string id, Vpc vpc)
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
                { "Kestrel__EndpointDefaults__Protocols", "Http1" },
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

        var internalListener = Service.AddListener("HttpListener", new BaseApplicationListenerProps
        {
            Port = 80,
            Protocol = ApplicationProtocol.HTTP,
            Open = true
        });
        internalListener.AddTargets("LambdaTarget", new AddApplicationTargetsProps
        {
            Targets = [new LambdaTarget(lambdaFunction)]
        });

        var certificateArn = StringParameter.ValueForStringParameter(this, "/scene-split/cert-arn");

        var certificate = Certificate.FromCertificateArn(this, "CompressionApiCertificate", certificateArn);

        var httpsListener = Service.AddListener("HttpsListener", new BaseApplicationListenerProps
        {
            Port = 443,
            Certificates = [new ListenerCertificate(certificate.CertificateArn)],
            Protocol = ApplicationProtocol.HTTPS,
            Open = true
        });

        httpsListener.AddTargets("LambdaTarget", new AddApplicationTargetsProps
        {
            Targets = [new LambdaTarget(lambdaFunction)]
        });
    }
}