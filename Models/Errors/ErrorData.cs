using AlfaMicroserviceMesh.Constants;

namespace AlfaMicroserviceMesh.Models.Errors;

public class ErrorData {
    public int Status { get; set; } = 500;
    public string Type { get; set; } = ErrorTypes.InternalServerError.Type;
    public string ErrorSource { get; set; } = $"{ServiceBroker.Service.Name}.{ServiceBroker.Service.InstanceID}";
    public object Error { get; set; } = new {};
}
