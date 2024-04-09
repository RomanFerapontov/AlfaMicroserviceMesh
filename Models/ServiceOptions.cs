namespace AlfaMicroserviceMesh.Models;

public class ServiceOptions {
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public bool Logging { get; set; } = false;
    public bool Metrics { get; set; } = false;
}
