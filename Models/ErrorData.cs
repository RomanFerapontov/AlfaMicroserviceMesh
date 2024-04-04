namespace AlfaMicroserviceMesh.Models;

public class ErrorData {
    public int Status { get; set; } = 500;
    public string Type { get; set; } = "INTERNAL_ERROR";
    public string ErrorSource { get; set; } = $"{ServiceBroker.ServiceName}.{ServiceBroker.ServiceInstance}";
    public List<string> Errors { get; set; } = [];
}
