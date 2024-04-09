namespace AlfaMicroserviceMesh.Models.Action;

public class ActionParams {
    public string Type { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public List<string>? Allowed { get; set; }
}
