namespace AlfaMicroserviceMesh.Models.Service;

public class ServiceData {
    public Dictionary<string, Dictionary<string, object>> Actions { get; set; } = [];
    public List<string> Events { get; set; } = [];
}
