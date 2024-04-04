using AlfaMicroserviceMesh.Models;
using AlfaMicroserviceMesh.Utils;
using System.Text;
using RabbitMQ.Client;
using Serilog;

namespace AlfaMicroserviceMesh.Communication;

public static class RabbitMQService {
    public static readonly string ExchangeName = "common-communications";
    private static IConnection? _connection;
    public static IModel? channel;
    private static readonly Context _context = NodesRegistry.selfContext;

    public static void Connect() {
        ConnectionFactory factory = new() { HostName = ServiceBroker.RabbitMQHost, Port = ServiceBroker.RabbitMQPort };

        string instanceId = _context.InstanceID;

        try {
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
