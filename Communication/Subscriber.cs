using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AlfaMicroserviceMesh.Models;
using Microsoft.Extensions.Hosting;
using AlfaMicroserviceMesh.Services;
using Serilog;

namespace AlfaMicroserviceMesh.Communication;

public class Subscriber(MessageProcessor eventProcessor) : BackgroundService {
    private readonly MessageProcessor _eventProcessor = eventProcessor;
    private readonly Context _context = Nodes.selfContext;

    protected override Task ExecuteAsync(CancellationToken token) {
        try {
        token.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(RabbitMQService.channel);

        consumer.Received += (ModuleHandle, ea) => HandleEvent(ea);

        RabbitMQService.channel.BasicConsume(queue: _context.InstanceID, autoAck: true, consumer: consumer);

        Log.Information($"Subscribed to {RabbitMQService.ExchangeName}");

        } catch (Exception) {
            Log.Error($"Connection to RabbitMQ on '{ServiceBroker.Service.Transport.Host}:{ServiceBroker.Service.Transport.Port}' cannot be established.");
            Environment.Exit(1);
        }

        return Task.CompletedTask;
    }

    private void HandleEvent(BasicDeliverEventArgs ea) {
        string message = Encoding.UTF8.GetString(ea.Body.ToArray());

        _eventProcessor?.ProcessMessage(message);
    }
}
