using AlfaMicroserviceMesh.Models.Errors;

namespace AlfaMicroserviceMesh.Exceptions;

public class MicroserviceException : Exception {
    private readonly string ServiceName = ServiceBroker.Service.Name;
    private readonly string ServiceInstance = ServiceBroker.Service.InstanceID;

    public ErrorData Info = new();

    public MicroserviceException(object message, ErrorResponseType errorType) {
        Info = new ErrorData {
            Status = errorType.Status,
            Type = errorType.Type,
            ErrorSource = $"{ServiceName}.{ServiceInstance}",
            Error = message,
        };
    }
    public MicroserviceException() { }
}