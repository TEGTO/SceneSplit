using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SQS;
using Constructs;
using SceneSplit.Configuration;

namespace SceneSplit.Cdk.Constructs;

public class SceneAnalysisLambdaConstruct : Construct
{
    public Function LambdaFunction { get; }

    public SceneAnalysisLambdaConstruct(
        Construct scope,
        string id,
        Vpc vpc,
        Bucket bucket,
        IQueue outputQueue)
        : base(scope, id)
    {
        var lambdaSecurityGroup = new SecurityGroup(this, "SceneAnalysisLambdaSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });
        var failureQueue = new Queue(this, "SceneAnalysisLambdaFailures");

        var account = Stack.Of(this).Account;
        var region = Stack.Of(this).Region;

        LambdaFunction = new Function(this, "SceneAnalysisLambda", new FunctionProps
        {
            FunctionName = "scene-split-scene-analysis-lambda",
            Runtime = Runtime.DOTNET_8,
            Handler = "SceneSplit.SceneAnalysisLambda::SceneSplit.SceneAnalysisLambda.Function::Handler",
            Code = Code.FromAsset("lambda-publish/SceneSplit.SceneAnalysisLambda.zip"),
            Timeout = Duration.Seconds(30),
            MemorySize = 128,
            Vpc = vpc,
            SecurityGroups = [lambdaSecurityGroup],
            Environment = new Dictionary<string, string>
            {
                { "DOTNET_ENVIRONMENT", "Production" },
                { SceneAnalysisLambdaConfigurationKeys.SQS_QUEUE_URL, outputQueue.QueueUrl },
                { SceneAnalysisLambdaConfigurationKeys.MAX_ITEMS, "5" },
                { SceneAnalysisLambdaConfigurationKeys.BEDROCK_MODEL_ID, $"arn:aws:bedrock:{region}:{account}:inference-profile/eu.amazon.nova-lite-v1:0" },
            },
            DeadLetterQueue = failureQueue,
            DeadLetterQueueEnabled = true
        });

        LambdaFunction.AddEventSource(new S3EventSource(bucket, new S3EventSourceProps
        {
            Events = [EventType.OBJECT_CREATED],
        }));

        bucket.GrantReadWrite(LambdaFunction);
        outputQueue.GrantSendMessages(LambdaFunction);

        LambdaFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions =
            [
                "bedrock:InvokeModel",
                "bedrock:InvokeModelWithResponseStream"
            ],
            Resources =
            [
                "arn:aws:bedrock:*::foundation-model/*",
                "arn:aws:bedrock:*:*:inference-profile/*"
            ]
        }));
    }
}