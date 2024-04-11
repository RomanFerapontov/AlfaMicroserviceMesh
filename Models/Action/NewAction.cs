using AlfaMicroserviceMesh.Models.ReqRes;
using AlfaMicroserviceMesh.Models.Service;

namespace AlfaMicroserviceMesh.Models.Action;

public class NewAction {
    public object? Route { get; set; }
    public object? Params { get; set; }
    public List<string>? Access { get; set; }
    public bool? Caching { get; set; }
    public RetryPolicy? RetryPolicy { get; set; }
    public int? RequestTimeout { get; set; }
    public Func<Context, Task<Response>>? Handler { get; set; }
}

