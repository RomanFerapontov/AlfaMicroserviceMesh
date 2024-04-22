using AlfaMicroserviceMesh.Models.Service;
using System.Text;
using RabbitMQ.Client;
using Serilog;
using AlfaMicroserviceMesh.Registry;

namespace AlfaMicroserviceMesh.Communication;

public static class RabbitMQService {
    public static readonly string ExchangeName = "common-communications";
    private static IConnection? _connection;
    public static IModel? channel;
    private static readonly Context _context = Services.selfContext;

    public static void Connect() {
        try {
        ConnectionFactory factory = new() {
            HostName = ServiceBroker.Service.Transport.Host,
            Port = int.Parse(ServiceBroker.Service.Transport.Port!),
        };

        string instanceId = _context.InstanceID;

            _connection = factory.CreateConnection();
            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;

            channel = _connection.CreateModel();

            channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic);
            channel.QueueDeclare(queue: instanceId, durable: true, exclusive: false, autoDelete: true, arguments: null);
            channel.QueueBind(queue: instanceId, exchange: ExchangeName, routingKey: instanceId);
            channel.QueueBind(queue: instanceId, exchange: ExchangeName, routingKey: "");
            channel.BasicQos(0, 1, false);

            Log.Information($"Queue {instanceId} is created");
        }
        catch (Exception ex) {
            Log.Information($"Could not connect to Message Bus: {ex.Message}");
        }
    }

    public static void PublishMessage(string message, string routingKey = "") {
        channel.BasicPublish(
            exchange: ExchangeName,
            routingKey: routingKey,
            basicProperties: null,
            body: Encoding.UTF8.GetBytes(message)
        );
    }

    private static void RabbitMQ_ConnectionShutdown(object? sender, ShutdownEventArgs e) =>
         Log.Information($"RabbitMQ Connection Shutdown");

    private static void Dispose() {
        if (channel!.IsOpen) {
            channel.Close();
            _connection!.Close();
        }
    }
}
