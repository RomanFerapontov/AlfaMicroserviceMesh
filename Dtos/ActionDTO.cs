namespace AlfaMicroserviceMesh.Dtos;

public class ActionDTO {
    public object Route { get; set; } = new();
    public object Params { get; set; } = new();
    public bool Caching { get; set; } = false;
    public List<string>? Access { get; set; }
}
