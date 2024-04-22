namespace AlfaMicroserviceMesh.Models.Service;

public class Service {
	public Dictionary<string, ServiceData> Instances { get; set; } = [];
	public string LastRequest { get; set; } = string.Empty;
}

