using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SQS;
using Constructs;
using SceneSplit.Configuration;

namespace SceneSplit.Cdk.Constructs;

public class ObjectImageCleanerLambdaConstruct : Construct
{
    public Function LambdaFunction { get; }

    public ObjectImageCleanerLambdaConstruct(
        Construct scope,
        string id,
        Vpc vpc,
        Bucket objectBucket)
        : base(scope, id)
    {
        var lambdaSecurityGroup = new SecurityGroup(this, "ObjectImageCleanerLambdaSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });
        var failureQueue = new Queue(this, "ObjectImageCleanerLambdaFailures");

        LambdaFunction = new Function(this, "ObjectImageCleanerLambda", new FunctionProps
        {
            FunctionName = "object-image-cleaner-lambda",
            Runtime = Runtime.DOTNET_8,
            Handler = "SceneSplit.ObjectImageCleanerLambda::SceneSplit.ObjectImageCleanerLambda.Function::Handler",
            Code = Code.FromAsset("lambda-publish/SceneSplit.ObjectImageCleanerLambda.zip"),
            Timeout = Duration.Seconds(30),
            MemorySize = 128,
            Vpc = vpc,
            SecurityGroups = [lambdaSecurityGroup],
            Environment = new Dictionary<string, string>
            {
                { "DOTNET_ENVIRONMENT", "Production" },
                { ObjectImageCleanerLambdaConfigurationKeys.MAX_KEYS_SEARCH_CONCURENCY, 16.ToString() },
                { ObjectImageCleanerLambdaConfigurationKeys.MAX_DELETE_BRANCH, 1000.ToString() },
            },
            DeadLetterQueue = failureQueue,
            DeadLetterQueueEnabled = true
        });

        LambdaFunction.AddEventSource(new S3EventSource(objectBucket, new S3EventSourceProps
        {
            Events = [EventType.OBJECT_CREATED],
        }));

        objectBucket.GrantReadWrite(LambdaFunction);
    }
}