namespace AlfaMicroserviceMesh.Models.Errors;

public class ErrorResponseType {
    public string Type { get; set; } = string.Empty;
    public int Status { get; set; } = 500;
}


/*
    public static readonly string InternalServerError = "INTERNAL_SERVER_ERROR"; 
    public static readonly string ValidationError = "VALIDATION_ERROR"; 
    public static readonly string ArgumentError = "ARGUMENT_ERROR";
    public static readonly string ServiceNotFound = "SERVICE_NOT_FOUND";
    public static readonly string ActionNotFound = "ACTION_NOT_FOUND"; 
    public static readonly string RequestTimeout = "REQUEST_TIMEOUT"; 
    public static readonly string Unauthorized = "AUTHORIZATION_ERROR"; 
    public static readonly string Forbidden = "PERMISSION_DENIED"; 
*/ 