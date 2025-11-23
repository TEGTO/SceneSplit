using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SceneSplit.Configuration;

namespace SceneSplit.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAwsOpenTelemetryMetrics(this IServiceCollection services, string serviceName)
    {
        var serviceNamespace = Environment.GetEnvironmentVariable(ObservabilityConfigurationKeys.OPENTELEMETRY_SERVICE_NAMESPACE)
            ?? "<empty>";
        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName, serviceNamespace);
                r.AddTelemetrySdk();
            })
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation();
                t.AddAWSInstrumentation();
                t.AddXRayTraceId();
                t.AddOtlpExporter();
            })
            .WithMetrics(m =>
            {
                m.AddAspNetCoreInstrumentation();
                m.AddOtlpExporter((exporterOptions, readerOptions) =>
                {
                    readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                });
            });

        return services;
    }
}