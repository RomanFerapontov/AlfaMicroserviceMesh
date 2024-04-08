using AlfaMicroserviceMesh.Communication;
using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Services;
using AlfaMicroserviceMesh.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace AlfaMicroserviceMesh;

public static class ServiceBroker {
    public static string ServiceName { get; set; } = string.Empty;
    public static string ServiceInstance { get; set; } = string.Empty;
    public static string RabbitMQHost { get; set; } = string.Empty;
    public static int RabbitMQPort { get; set; }

    public static void CreateService(this WebApplicationBuilder builder, ServiceOptions configuration) {
        AddServices(builder);

        string transportUrl = configuration.Transport.Split("://")[^1];
        string[] transport = transportUrl.Split(":");

        ServiceName = configuration.Name;
        ServiceInstance = Guid.NewGuid().ToString();
        RabbitMQHost = transport[^2];
        RabbitMQPort = int.Parse(transport[^1]);

        RabbitMQService.Connect();
    }

    public static void AddServices(this WebApplicationBuilder builder) {
        builder.Host.UseSerilog((context, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration));

        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<MessageProcessor>();
        builder.Services.AddSingleton<HealthChecker>();
        builder.Services.AddHostedService<Subscriber>();
        builder.Services.AddHostedService<HealthChecker>();
    }
}

