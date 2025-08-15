using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    public static IServiceCollection AddServiceDefaults(this IServiceCollection services, IConfiguration? configuration = null)
    {
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        services.AddHealthChecks();
        services.AddHttpClient();
        return services;
    }

    public static IHostApplicationBuilder AddObservabilityLogging(this IHostApplicationBuilder builder, string serviceName)
    {
        // Add OpenTelemetry logging
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"));

            // Add OTLP exporter for logs
            var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
            if (!string.IsNullOrEmpty(otlpEndpoint))
            {
                logging.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                });
            }

            // Add console exporter for development
            if (builder.Environment.IsDevelopment())
            {
                logging.AddConsoleExporter();
            }
        });

        return builder;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, string serviceName, IConfiguration configuration, IHostEnvironment environment, string[]? additionalActivitySources = null)
    {
        // Add OpenTelemetry for observability
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource("*") // Add all activity sources
                    .AddSource("JsonRpcTracing") // Add JSON-RPC tracing
                    .AddSource("ModelContextProtocol") // Specific MCP sources
                    .AddSource("ModelContextProtocol.AspNetCore")
                    .AddSource("A2A") // Add A2A activity sources
                    .AddSource("TaskManager")
                    .AddSource("A2AJsonRpcProcessor")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Add any additional activity sources
                if (additionalActivitySources != null)
                {
                    foreach (var source in additionalActivitySources)
                    {
                        tracing.AddSource(source);
                    }
                }

                // Only add console exporter in development mode
                if (environment.IsDevelopment() &&
                    configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter", false))
                {
                    tracing.AddConsoleExporter();
                }

                // Always add OTLP exporter if endpoint is configured
                var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter("*") // Add all meters
                    .AddMeter("ModelContextProtocol") // Specific MCP meters
                    .AddMeter("ModelContextProtocol.AspNetCore")
                    .AddMeter("A2A") // Add A2A meters
                    .AddMeter("TaskManager")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                // Only add console exporter in development mode with reduced verbosity
                if (environment.IsDevelopment() &&
                    configuration.GetValue<bool>("OpenTelemetry:EnableConsoleExporter", false))
                {
                    metrics.AddConsoleExporter((exporterOptions, metricReaderOptions) =>
                    {
                        // Reduce console output frequency
                        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 30000; // 30 seconds
                    });
                }

                // Always add OTLP exporter if endpoint is configured
                var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(otlpEndpoint);
                    });
                }
            })
            .WithLogging(); // Add logging as shown in the reference

        return services;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app, string? serviceName = null)
    {
        serviceName ??= app.Environment.ApplicationName;
        app.MapGet("/health/live", () => Results.Ok(new
        {
            Status = "Live",
            Service = serviceName,
            Timestamp = DateTime.UtcNow
        })).AllowAnonymous();

        app.MapGet("/health/ready", async (HealthCheckService hcs) =>
        {
            var report = await hcs.CheckHealthAsync();
            var response = new
            {
                Status = report.Status.ToString(),
                Service = serviceName,
                Timestamp = DateTime.UtcNow,
                Results = report.Entries.ToDictionary(k => k.Key, v => v.Value.Status.ToString())
            };
            return report.Status == HealthStatus.Healthy ? Results.Ok(response) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous();

        app.MapGet("/", () => Results.Ok(new { Service = serviceName, Status = "Running", Timestamp = DateTime.UtcNow }))
           .AllowAnonymous();
        return app;
    }
}
