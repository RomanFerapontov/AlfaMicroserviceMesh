namespace AlfaMicroserviceMesh.Models.Errors;

public class ErrorValidationData {
    public string? Field { get; set; }
    public object? Value { get; set; }
    public string? Message { get; set; }
}
