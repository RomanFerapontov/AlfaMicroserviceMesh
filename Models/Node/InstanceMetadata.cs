using AlfaMicroserviceMesh.Dtos;

namespace AlfaMicroserviceMesh.Models.Node;

public class InstanceMetadata {
    public Dictionary<string, ActionDTO> Actions { get; set; } = [];
}
