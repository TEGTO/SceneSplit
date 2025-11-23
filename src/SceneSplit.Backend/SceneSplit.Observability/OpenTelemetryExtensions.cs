using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static SceneSplit.Configuration.ObservabilityConfigurationKeys;

namespace SceneSplit.Observability;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddAwsOpenTelemetryMetrics(this IServiceCollection services)
    {
        var serviceName = Environment.GetEnvironmentVariable(OPENTELEMETRY_SERVICE_NAME)
            ?? "<empty>";
        var serviceNamespace = Environment.GetEnvironmentVariable(OPENTELEMETRY_SERVICE_NAMESPACE)
            ?? "<empty>";

        services.AddOpenTelemetry()
            .ConfigureResource(r =>
            {
                r.AddService(serviceName, serviceNamespace);
                r.AddTelemetrySdk();
            })
            .WithTracing(t =>
            {
                t.AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments(HEALTH_ENDPOINT);
                });
                t.AddHttpClientInstrumentation();
                t.AddAWSInstrumentation();
                t.AddXRayTraceId();
                t.AddOtlpExporter();
            })
            .WithMetrics(m =>
            {
                m.AddHttpClientInstrumentation();
                m.AddAspNetCoreInstrumentation();
                m.AddOtlpExporter((exporterOptions, readerOptions) =>
                {
                    readerOptions.TemporalityPreference = MetricReaderTemporalityPreference.Delta;
                });
            });

        return services;
    }
}