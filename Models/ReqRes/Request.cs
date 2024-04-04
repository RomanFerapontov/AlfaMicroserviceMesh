namespace AlfaMicroserviceMesh.Models.ReqRes;

public class Request {
    public object Parameters { get; set; } = new();
    public ClaimData Access {get; set;} = new();
}
