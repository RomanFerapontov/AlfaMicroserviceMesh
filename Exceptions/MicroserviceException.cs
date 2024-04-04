using AlfaMicroserviceMesh.Models;

namespace AlfaMicroserviceMesh.Exceptions;

public class MicroserviceException : Exception {
    private readonly string ServiceName = ServiceBroker.ServiceName;
    private readonly string ServiceInstance = ServiceBroker.ServiceInstance;

    public ErrorData Info = new();

    public MicroserviceException(List<string> messages, int status = 500, string type = "INTERNAL_ERROR") {
        Info = new ErrorData {
            Status = status,
            Type = type,
            ErrorSource = $"{ServiceName}.{ServiceInstance}",
            Errors = messages,
        };
    }
    public MicroserviceException() { }
}