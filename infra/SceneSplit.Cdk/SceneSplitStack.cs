using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.ServiceDiscovery;
using Constructs;
using SceneSplit.Cdk.Helpers;
using SceneSplit.Configuration;
using Cluster = Amazon.CDK.AWS.ECS.Cluster;
using ClusterProps = Amazon.CDK.AWS.ECS.ClusterProps;

namespace SceneSplit.Cdk;

public class SceneSplitStack : Stack
{
    private const string SERVICE_NAMESPACE = "scene-split";

    private RestApi apiGateway = default!;
    private ApplicationLoadBalancedFargateService apiAlbService = default!;

    internal SceneSplitStack(Construct scope, string id, IStackProps props = null!) : base(scope, id, props)
    {
        var vpc = new Vpc(this, $"Vpc", new VpcProps { MaxAzs = 2 });

        var cluster = new Cluster(this, $"Cluster", new ClusterProps
        {
            Vpc = vpc,
            ClusterName = "scene-split-cluster",
            DefaultCloudMapNamespace = new CloudMapNamespaceOptions
            {
                Name = SERVICE_NAMESPACE,
                Type = NamespaceType.DNS_PRIVATE,
                Vpc = vpc
            },
        });

        AddApi(cluster, vpc);

        AddApiGateway();

        //var apiHubEndpoint = $"{apiGateway.Url}hubs/scene-split/";
        var apiHubEndpoint = $"http://{apiAlbService.LoadBalancer.LoadBalancerDnsName}:8080/hubs/scene-split/";
        AddFrontend(cluster, apiHubEndpoint);
    }

    private void AddApi(Cluster cluster, Vpc vpc)
    {
        var apiSecGroup = new SecurityGroup(this, "ApiServiceSecurityGroup", new SecurityGroupProps()
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });

        apiSecGroup.Connections.AllowFrom(
            Peer.AnyIpv4(),
            Port.Tcp(8080)
        );

        apiAlbService = new ApplicationLoadBalancedFargateService(this, "ApiService",
            new ApplicationLoadBalancedFargateServiceProps
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
                MemoryLimitMiB = 1024,
                PublicLoadBalancer = true,
                Cpu = 512,
                ServiceName = "api",
                CloudMapOptions = new CloudMapOptions
                {
                    Name = "api",
                    DnsRecordType = DnsRecordType.A,
                    DnsTtl = Duration.Seconds(60)
                },
                HealthCheck = TaskHelpers.AddHealthCheckForTask("8080/health"),
                MinHealthyPercent = 100,
                MaxHealthyPercent = 200
            });

        apiAlbService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
        {
            Path = "/health",
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            HealthyHttpCodes = "200-299",
            HealthyThresholdCount = 2,
            UnhealthyThresholdCount = 3
        });
    }

    private void AddApiGateway()
    {
        apiGateway = new RestApi(this, "ApiGateway", new RestApiProps
        {
            RestApiName = "SceneSplit API",
            Description = "Api Gateway routing to ECS API services",
            DefaultCorsPreflightOptions = new CorsOptions
            {
                AllowOrigins = Cors.ALL_ORIGINS,
                AllowMethods = Cors.ALL_METHODS,
            },
        });

        var integration = new Integration(new IntegrationProps
        {
            Type = IntegrationType.HTTP_PROXY,
            IntegrationHttpMethod = "ANY",
            Uri = $"http://{apiAlbService.LoadBalancer.LoadBalancerDnsName}/{{proxy}}",
            Options = new IntegrationOptions
            {
                PassthroughBehavior = PassthroughBehavior.WHEN_NO_MATCH,
                RequestParameters = new Dictionary<string, string>
                {
                    { "integration.request.path.proxy", "method.request.path.proxy" }
                }
            }
        });

        apiGateway.Root.AddProxy(new ProxyResourceOptions
        {
            DefaultIntegration = integration,
            AnyMethod = true,
            DefaultMethodOptions = new MethodOptions
            {
                RequestParameters = new Dictionary<string, bool>
                {
                    { "method.request.path.proxy", true }
                }
            }
        });
    }

    private void AddFrontend(Cluster cluster, string apiHubEndpoint)
    {
        var frontendService = new ApplicationLoadBalancedFargateService(this, "FrontendService",
            new ApplicationLoadBalancedFargateServiceProps
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

        frontendService.TargetGroup.ConfigureHealthCheck(new Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck
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