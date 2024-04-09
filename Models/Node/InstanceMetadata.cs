namespace AlfaMicroserviceMesh.Models.Node;

public class InstanceMetadata {
    public Dictionary<string, Dictionary<string, object>> Actions { get; set; } = [];
    public List<string> Events { get; set; } = [];
}
