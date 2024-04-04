using AlfaMicroserviceMesh.Models.Node;
using AlfaMicroserviceMesh.Models.ReqRes;

namespace AlfaMicroserviceMesh.Models;

public class Context {
    public string NodeName { get; set; } = string.Empty;
    public string InstanceID { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string RequestID { get; set; } = string.Empty;
    public InstanceMetadata Metadata { get; set; } = new();
    public Request Request { get; set; } = new();
    public Response Response { get; set; } = new();
}

