using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using AlfaMicroserviceMesh.Utils;
using AlfaMicroserviceMesh.Models;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace AlfaMicroserviceMesh.Communication;

public class Subscriber(MessageProcessor eventProcessor) : BackgroundService {
    private readonly MessageProcessor _eventProcessor = eventProcessor;
    private readonly Context _context = NodesRegistry.selfContext;

    protected override Task ExecuteAsync(CancellationToken token) {
        token.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(RabbitMQService.channel);

        consumer.Received += (ModuleHandle, ea) => HandleEvent(ea);

        RabbitMQService.channel.BasicConsume(queue: _context.InstanceID, autoAck: true, consumer: consumer);

        Log.Information($"Subscribed to {RabbitMQService.ExchangeName}");

        return Task.CompletedTask;
    }

    private void HandleEvent(BasicDeliverEventArgs ea) {
        string message = Encoding.UTF8.GetString(ea.Body.ToArray());

        _eventProcessor?.ProcessMessage(message);
    }
}
