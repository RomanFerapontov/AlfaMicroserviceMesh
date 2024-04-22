using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Models.Service;
using AlfaMicroserviceMesh.Registry;
using Microsoft.Extensions.Hosting;

namespace AlfaMicroserviceMesh.Communication;

public class HealthChecker : BackgroundService {
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMilliseconds(5000));
    private readonly Context _context = Services.selfContext;

    protected override async Task ExecuteAsync(CancellationToken token) {
        while (await _timer.WaitForNextTickAsync(token) && !token.IsCancellationRequested) {
            RabbitMQService.PublishMessage(await new {
                Event = "discover",
                _context.ServiceName,
                _context.InstanceID,
                _context.Metadata,
            }.SerializeAsync());
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken) {
        RabbitMQService.PublishMessage(await new {
            Event = "dispose",
            _context.ServiceName,
            _context.InstanceID,
        }.SerializeAsync());

        await base.StopAsync(cancellationToken);
    }
}
