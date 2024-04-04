
namespace AlfaMicroserviceMesh.Models.Node;

public class Node {
	public Dictionary<string, InstanceMetadata> Instances { get; set; } = [];
	public string LastRequest { get; set; } = string.Empty;
}

