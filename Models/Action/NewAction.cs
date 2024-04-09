namespace AlfaMicroserviceMesh.Models.Action;

public class NewAction {
    public object? Route { get; set; }
    public object? Params { get; set; }
    public List<string>? Access { get; set; }
    public bool? Caching { get; set; }
    public Func<Context, Task<object>>? Handler { get; set; }
}

