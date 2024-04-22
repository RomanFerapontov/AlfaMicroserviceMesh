using AlfaMicroserviceMesh.Models.Errors;

namespace AlfaMicroserviceMesh.Models.ReqRes;

public class Response {
    public object Data { get; set; }  = new {};
    public ErrorData? Error { get; set; }
}
