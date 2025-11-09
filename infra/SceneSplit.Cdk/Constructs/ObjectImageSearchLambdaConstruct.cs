using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Constructs;
using SceneSplit.Configuration;

namespace SceneSplit.Cdk.Constructs;

public class ObjectImageSearchLambdaConstruct : Construct
{
    public Function LambdaFunction { get; }

    public ObjectImageSearchLambdaConstruct(
        Construct scope,
        string id,
        Vpc vpc,
        IQueue objectQueue,
        Bucket outputBucket,
        string compressionApiUrl)
        : base(scope, id)
    {
        var lambdaSecurityGroup = new SecurityGroup(this, "ObjectImageSearchLambdaSecurityGroup", new SecurityGroupProps
        {
            AllowAllOutbound = true,
            Vpc = vpc
        });
        var failureQueue = new Queue(this, "ObjectImageSearchLambdaFailures");

        var ssmImageSearchApiKey = StringParameter
            .FromStringParameterName(this, "ImageSearchApiKeyParameter", "/scene-split/image-search-api-key")
            .StringValue;

        LambdaFunction = new Function(this, "ObjectImageSearchLambda", new FunctionProps
        {
            FunctionName = "object-image-search-lambda",
            Runtime = Runtime.DOTNET_8,
            Handler = "SceneSplit.ObjectImageSearchLambda::SceneSplit.ObjectImageSearchLambda.Function::Handler",
            Code = Code.FromAsset("lambda-publish/SceneSplit.ObjectImageSearchLambda.zip"),
            Timeout = Duration.Seconds(30),
            MemorySize = 128,
            Vpc = vpc,
            SecurityGroups = [lambdaSecurityGroup],
            Environment = new Dictionary<string, string>
            {
                { "DOTNET_ENVIRONMENT", "Production" },
                { ObjectImageSearchLambdaConfigurationKeys.BUCKET_NAME, outputBucket.BucketName },
                { ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_KEY, ssmImageSearchApiKey },
                { ObjectImageSearchLambdaConfigurationKeys.IMAGE_SEARCH_API_ENDPOINT, "https://api.unsplash.com/search/photos" },
                { ObjectImageSearchLambdaConfigurationKeys.COMPRESSION_API_URL, compressionApiUrl },
                { ObjectImageSearchLambdaConfigurationKeys.MAX_IMAGE_SIZE,  (10 * 1024 * 1024).ToString()  },
                { ObjectImageSearchLambdaConfigurationKeys.RESIZE_WIDTH, "1024" },
                { ObjectImageSearchLambdaConfigurationKeys.RESIZE_HEIGHT, "1024" },
                { ObjectImageSearchLambdaConfigurationKeys.IMAGE_QUALITY_COMPRESSION, "75" },
            },
            DeadLetterQueue = failureQueue,
            DeadLetterQueueEnabled = true
        });

        LambdaFunction.AddEventSource(new SqsEventSource(objectQueue, new SqsEventSourceProps
        {
            Enabled = true,
        }));

        outputBucket.GrantReadWrite(LambdaFunction);
    }
}