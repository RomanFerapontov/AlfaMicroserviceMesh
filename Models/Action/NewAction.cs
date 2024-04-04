namespace AlfaMicroserviceMesh.Models.Action;

public class NewAction {
    public object Route { get; set; } = new();
    public object Params { get; set; } = new();
    public List<string>? Access { get; set; } = ["ALL"];
    public Func<Context, Task<object>>? Handler { get; set; }
}

