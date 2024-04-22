using AlfaMicroserviceMesh.Models.Errors;

namespace AlfaMicroserviceMesh.Constants;

public static class ErrorTypes {
    public static readonly ErrorResponseType InternalServerError = new() {Type = "INTERNAL_SERVER_ERROR"}; 
    public static readonly ErrorResponseType ValidationError = new() {Type = "VALIDATION_ERROR", Status = 422}; 
    public static readonly ErrorResponseType ArgumentError = new() {Type = "ARGUMENT_ERROR", Status = 409};
    public static readonly ErrorResponseType ServiceNotFound = new() {Type = "SERVICE_NOT_FOUND", Status = 404};
    public static readonly ErrorResponseType ActionNotFound = new() {Type = "ACTION_NOT_FOUND", Status = 404}; 
    public static readonly ErrorResponseType RequestTimeout = new() {Type = "REQUEST_TIMEOUT", Status = 504}; 
    public static readonly ErrorResponseType Unauthorized = new() {Type = "AUTHORIZATION_ERROR", Status = 401}; 
    public static readonly ErrorResponseType Forbidden = new() {Type = "PERMISSION_DENIED", Status = 403}; 
}
