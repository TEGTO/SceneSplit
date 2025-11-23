using Amazon.CDK;

namespace SceneSplit.Cdk.Helpers;

public static class OtelCollectorHelper
{
    public static string GetConfiguration(string logGroupName)
    {
        const string OTEL_COLLECTOR_YAML_WITH_TRACES =
        @"
            receivers:
              otlp:
                protocols:
                  grpc: {}
                  http: {}

            exporters:
              debug:
                verbosity: basic
              awsxray:
                index_all_attributes: true
              awsemf:
                log_group_name: '${Ecs_Service_LogGroupName}'

            service:
              pipelines:
                traces:
                  receivers: [otlp]
                  exporters: [debug, awsxray]
                metrics:
                  receivers: [otlp]
                  exporters: [awsemf]
              telemetry:
                logs:
                  level: error
        ";

        var replaceOptions = new Dictionary<string, string>
        {
            { "Ecs_Service_LogGroupName", logGroupName }
        };

        return Fn.Sub(OTEL_COLLECTOR_YAML_WITH_TRACES, replaceOptions).ToString();
    }
}