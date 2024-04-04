namespace AlfaMicroserviceMesh.Dtos;

public class ActionDTO {
    public object Route { get; set; } = new();
    public object Params { get; set; } = new();
    public List<string>? Access { get; set; }
}
