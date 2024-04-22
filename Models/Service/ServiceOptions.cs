using AlfaMicroserviceMesh.Models.Service.Handler;

namespace AlfaMicroserviceMesh.Models.Service;

public class ServiceOptions {
    public string Name { get; set; } = string.Empty;
    public string InstanceID { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public ServerAddress Transport { get; set; } = new();
    public RetryPolicy RetryPolicy { get; set; } = new();
    public bool Logging { get; set; } = false;
    public bool Metrics { get; set; } = false;
    public int RequestTimeout { get; set; } = 5000;
}
