using Amazon.CDK;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using SceneSplit.Configuration;

namespace SceneSplit.Cdk.Helpers;

public static class TaskHelpers
{
    public static HealthCheck AddHealthCheckForTask(string relativeUrl)
    {
        return new HealthCheck
        {
            Command = ["CMD-SHELL", $"curl -f http://localhost:{relativeUrl} || exit 1"],
            Interval = Duration.Seconds(30),
            Timeout = Duration.Seconds(5),
            Retries = 3,
            StartPeriod = Duration.Seconds(10)
        };
    }

    public static T AddOtelCollectorSidecar<T>(
        T props,
        string serviceNamespace,
        string collectorConfig,
        string containerName = "otel-collector") where T : IFargateServiceBaseProps
    {
        var defaultContainer = props.TaskDefinition?.DefaultContainer;

        if (defaultContainer == null)
        {
            return props;
        }

        defaultContainer.AddEnvironment(ObservabilityConfigurationKeys.OPENTELEMETRY_SERVICE_NAMESPACE, serviceNamespace);
        defaultContainer.AddEnvironment("AWS_EMF_ENVIRONMENT", "Local");

        props.TaskDefinition!.AddToTaskRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Resources = ["*"],
            Actions =
            [
                "logs:PutLogEvents",
                "logs:CreateLogGroup",
                "logs:CreateLogStream",
                "logs:DescribeLogStreams",
                "logs:DescribeLogGroups",
                "xray:PutTraceSegments",
                "xray:PutTelemetryRecords",
                "xray:GetSamplingRules",
                "xray:GetSamplingTargets",
                "xray:GetSamplingStatisticSummaries"
            ]
        }));

        var serviceLogGroupName = defaultContainer.LogDriverConfig?.Options?["awslogs-group"];

        var serviceLogGroup = !string.IsNullOrEmpty(serviceLogGroupName)
            ? LogGroup.FromLogGroupName(props.TaskDefinition, "OtelCollectorLogGroup", serviceLogGroupName)
            : null;

        var streamPrefix = defaultContainer.LogDriverConfig?.Options?["awslogs-stream-prefix"] ?? string.Empty;

        var serviceLogDriver = new AwsLogDriver(new AwsLogDriverProps
        {
            LogGroup = serviceLogGroup,
            StreamPrefix = streamPrefix
        });

        var container = props.TaskDefinition.AddContainer(containerName, new ContainerDefinitionOptions
        {
            ContainerName = containerName,
            Command = ["--config=/etc/ecs/ecs-default-config.yaml"],
            Image = ContainerImage.FromRegistry("public.ecr.aws/aws-observability/aws-otel-collector:v0.42.0"),
            Essential = false,
            MemoryReservationMiB = 128,
            ReadonlyRootFilesystem = true,
            Logging = serviceLogDriver,
            Environment = new Dictionary<string, string>
            {
                ["AOT_CONFIG_CONTENT"] = collectorConfig,
            },
            PortMappings =
            [
                new PortMapping
                {
                    ContainerPort = 4317, HostPort = 4317, Protocol = Protocol.TCP, AppProtocol = AppProtocol.Grpc,
                    Name = "grpc"
                },
                new PortMapping
                {
                    ContainerPort = 4318, HostPort = 4318, Protocol = Protocol.TCP, AppProtocol = AppProtocol.Http,
                    Name = "http"
                },
            ]
        });

        defaultContainer.AddContainerDependencies(
           new ContainerDependency
           {
               Container = container,
               Condition = ContainerDependencyCondition.START
           });

        return props;
    }
}