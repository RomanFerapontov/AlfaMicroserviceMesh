using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Models.Service;

public class Context {
    public string ServiceName { get; set; } = string.Empty;
    public string InstanceID { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string RequestID { get; set; } = string.Empty;
    public ServiceData Metadata { get; set; } = new();
    public Request Request { get; set; } = new();
    public Response Response { get; set; } = new();
}

