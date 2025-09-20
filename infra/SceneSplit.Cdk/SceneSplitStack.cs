using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.ServiceDiscovery;
using Constructs;

namespace SceneSplit.Cdk;

public class SceneSplitStack : Stack
{
    internal SceneSplitStack(Construct scope, string id, SceneSplitStackProps props = null!) : base(scope, id, props)
    {
        var vpc = new Vpc(this, "MyVpc", new VpcProps
        {
            MaxAzs = 2
        });

        var namespaceName = "scene-split";
        var cluster = new Cluster(this, "MyCluster", new ClusterProps
        {
            Vpc = vpc,
            DefaultCloudMapNamespace = new CloudMapNamespaceOptions
            {
                Name = namespaceName,
                Type = NamespaceType.DNS_PRIVATE,
                Vpc = vpc
            }
        });

        AddApiGateway();

        AddApi();

        var frontendName = "frontend";
        AddFrontend(props, cluster, frontendName);
    }

    private void AddApiGateway()
    {
        throw new NotImplementedException();
    }

    private void AddApi()
    {
        throw new NotImplementedException();
    }

    private void AddFrontend(SceneSplitStackProps props, Cluster cluster, string serviceName)
    {
        var fargateService = new ApplicationLoadBalancedFargateService(this, $"{serviceName}Service", new ApplicationLoadBalancedFargateServiceProps
        {
            Cluster = cluster,
            DesiredCount = 1,
            TaskImageOptions = new ApplicationLoadBalancedTaskImageOptions
            {
                Image = ContainerImage.FromAsset(props.FrontendDockerfileDirectory, new AssetImageProps
                {
                    File = props.FrontendDockerfileName,
                    BuildArgs = new Dictionary<string, string>
                    {
                        { "ENV", "production" },
                        { "HUB_URL", "https://api.mycompany.com/hubs/scene-split" },
                        { "MAX_FILE_SIZE", (10 * 1024 * 1024).ToString() },
                        { "ALLOWED_IMAGE_TYPES", "image/png,image/jpeg" }
                }
                }),
                ContainerPort = 80,
                LogDriver = LogDriver.AwsLogs(new AwsLogDriverProps
                {
                    StreamPrefix = $"{serviceName}ServiceLogs"
                }),
            },
            MemoryLimitMiB = 1024,
            Cpu = 512,
            ServiceName = serviceName,
            CloudMapOptions = new CloudMapOptions
            {
                Name = serviceName,
                DnsRecordType = DnsRecordType.A,
                DnsTtl = Duration.Seconds(60)
            },
            AssignPublicIp = true,
            PublicLoadBalancer = true,
            TaskSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
        });

        var cfnService = fargateService.Service.Node.DefaultChild as Amazon.CDK.AWS.ECS.CfnService;
        if (cfnService != null)
        {
            cfnService.AddPropertyOverride("NetworkConfiguration.AwsvpcConfiguration.AssignPublicIp", "ENABLED");
        }

        fargateService.TaskDefinition.TaskRole.AddManagedPolicy(
            ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"));

        fargateService.Service.Connections.SecurityGroups[0].AddIngressRule(
            Peer.AnyIpv4(),
            Port.Tcp(80),
            "Allow public HTTP access over IPv4"
        );

        fargateService.Service.Connections.SecurityGroups[0].AddIngressRule(
            Peer.AnyIpv6(),
            Port.Tcp(80),
            "Allow public HTTP access over IPv6"
        );
    }
}