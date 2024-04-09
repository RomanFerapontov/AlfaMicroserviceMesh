using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Services;
using AlfaMicroserviceMesh.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using Serilog;

namespace AlfaMicroserviceMesh;

public static class ServiceBroker {
    public static string ServiceName { get; set; } = string.Empty;
    public static string ServiceInstance { get; set; } = string.Empty;
    public static string RabbitMQHost { get; set; } = string.Empty;
    public static int RabbitMQPort { get; set; }

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

        string transportUrl = configuration.Transport.Split("://")[^1];
        string[] transport = transportUrl.Split(":");

        ServiceName = configuration.Name;
        ServiceInstance = Guid.NewGuid().ToString();
        RabbitMQHost = transport[^2];
        RabbitMQPort = int.Parse(transport[^1]);

        RabbitMQService.Connect();
    }
}

