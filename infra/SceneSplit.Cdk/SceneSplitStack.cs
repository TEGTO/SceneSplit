using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.S3;
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

        var sceneImageBucket = new Bucket(this, "SceneImageBucket", new BucketProps
        {
            BucketName = "scene-split-scene-images",
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            BlockPublicAccess = BlockPublicAccess.BLOCK_ALL,
            Versioned = false,
            Encryption = BucketEncryption.S3_MANAGED
        });

        var compressionApiService = new CompressionApiServiceConstruct(this, "CompressionApiServiceConstruct", cluster, vpc);

        var compressionApiUrl = $"https://{compressionApiService.FargateService.LoadBalancer.LoadBalancerDnsName}";
        var apiService = new ApiServiceConstruct(
            this,
            "ApiServiceConstruct",
            cluster,
            vpc,
            compressionApiUrl,
            compressionApiService.FargateService.Service.Connections.SecurityGroups[0],
            sceneImageBucket.BucketName
        );

        sceneImageBucket.GrantReadWrite(apiService.FargateService.TaskDefinition.TaskRole);

        var apiEndpoint = $"http://{apiService.FargateService.LoadBalancer.LoadBalancerDnsName}";
        _ = new FrontendServiceConstruct(this, "FrontendServiceConstruct", cluster, vpc, apiEndpoint,
            apiService.FargateService.Service.Connections.SecurityGroups[0]);
    }
}