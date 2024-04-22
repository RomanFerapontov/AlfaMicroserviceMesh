namespace AlfaMicroserviceMesh.Constants;

public static class ErrorMessages {
    public static readonly string EmailFormat  = "Value must be in the correct format: 'name@example.com'";
    public static readonly string PasswordFormat = "Value must be at least 8 characters long and contain uppercase, lowercase, and numeric characters";
    public static readonly string NotAllowed = $"Allowed values: ";
    public static readonly string Type = $"Value must be type of ";
    public static readonly string Required = $"A required parameter must be passed";
}
