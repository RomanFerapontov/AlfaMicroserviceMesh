using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Models.Service;
using AlfaMicroserviceMesh.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Serilog;

namespace AlfaMicroserviceMesh;

public static class ServiceBroker {
    public static readonly ServiceOptions Service = new();

    public static void CreateService(this WebApplicationBuilder builder, ServiceOptions configuration) {
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<MessageProcessor>();
        builder.Services.AddSingleton<HealthChecker>();
        builder.Services.AddHostedService<Subscriber>();
        builder.Services.AddHostedService<HealthChecker>();

        if (configuration.Metrics) {
            builder.Services.AddOpenTelemetry()
        .WithMetrics(opt => {
            opt.AddPrometheusExporter();

            opt.AddMeter(
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel");

            opt.AddView(
                "request-processing",
                new ExplicitBucketHistogramConfiguration() {
                    Boundaries = [10, 20]
                });
            });
        }

        if (configuration.Logging) {
            builder.Host.UseSerilog((context, loggerConfig) =>
                loggerConfig.ReadFrom.Configuration(context.Configuration));
        }

        Service.Name = configuration.Name;
        Service.InstanceID = Guid.NewGuid().ToString();
        Service.Transport = configuration.Transport;
        Service.RetryPolicy = configuration.RetryPolicy;
        Service.RequestTimeout = configuration.RequestTimeout;

        RabbitMQService.Connect();
    }
}

