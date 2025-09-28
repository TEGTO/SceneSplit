using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ServiceDiscovery;
using Constructs;
using SceneSplit.Cdk.Constructs;
using Cluster = Amazon.CDK.AWS.ECS.Cluster;
using ClusterProps = Amazon.CDK.AWS.ECS.ClusterProps;

namespace SceneSplit.Cdk;

public class SceneSplitStack : Stack
{
    internal SceneSplitStack(Construct scope, string id, IStackProps props = null!) : base(scope, id, props)
    {
        var vpc = new Vpc(this, "Vpc", new VpcProps { MaxAzs = 2 });

        var cluster = new Cluster(this, "Cluster", new ClusterProps
        {
            Vpc = vpc,
            ClusterName = "scene-split-cluster",
            DefaultCloudMapNamespace = new CloudMapNamespaceOptions
            {
                Name = "scene-split",
                Type = NamespaceType.DNS_PRIVATE,
                Vpc = vpc
            },
        });

        var apiService = new ApiServiceConstruct(this, "ApiServiceConstruct", cluster, vpc);

        var apiEndpoint = $"http://{apiService.Service.LoadBalancer.LoadBalancerDnsName}";
        _ = new FrontendServiceConstruct(this, "FrontendServiceConstruct", cluster, apiEndpoint,
            apiService.Service.Service.Connections.SecurityGroups[0], vpc);
    }
}