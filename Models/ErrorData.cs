namespace AlfaMicroserviceMesh.Models;

public class ErrorData {
    public int Status { get; set; } = 500;
    public string Type { get; set; } = "INTERNAL_ERROR";
    public string ErrorSource { get; set; } = $"{ServiceBroker.Service.Name}.{ServiceBroker.Service.InstanceID}";
    public List<string> Errors { get; set; } = [];
}
