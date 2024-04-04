using System.Text.Json;
using System.Text.RegularExpressions;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Models.Action;

namespace AlfaMicroserviceMesh.Validator;

public static class CustomValidator {
    private static List<string> Errors { get; set; } = [];

    public static async Task<object> ValidateParams(object requestParams, Dictionary<string, ActionParams> actionParams) {
        string paramsString = await requestParams.SerializeAsync();

        var paramsDict = await paramsString.DeserializeAsync<Dictionary<string, object>>();

        CheckRequiredParams([.. paramsDict.Keys], GetRequiredParamsList(actionParams));

        var parameters = JsonDocument.Parse(paramsString);

        foreach (var param in parameters.RootElement.EnumerateObject()) {
            string requestParam = param.Value.ToString();
            string requestParamType = param.Value.ValueKind.ToString();

            if (actionParams.TryGetValue(param.Name, out ActionParams? actionParam)) {
                var actionParamType = actionParam.Type;

                var allowedValues = actionParam.Allowed;

                if (allowedValues != null) {
                    actionParamType = "String";
                    if (!allowedValues.Contains(requestParam)) {
                        Errors.Add($"Value must be equal: {string.Join("/", allowedValues)}. (Parameter '{param.Name}')");
                    }
                }
                if (actionParamType == "Email") {
                    if (IsEmail(requestParam) == false) {
                        Errors.Add($"Value must be in the correct format (e.g., name@example.com). (Parameter '{param.Name}')");
                    }
                }
                else if (actionParamType == "Password") {
                    if (IsPassword(requestParam) == false) {
                        Errors.Add($"Value must be at least 8 characters long and contain uppercase, lowercase, and numeric characters. (Parameter '{param.Name}')");
                    }
                }
                else if (actionParamType != requestParamType) {
                    Errors.Add($"Value must be type of {actionParamType}. (Parameter '{param.Name}')");
                }
            }
        }

        if (Errors.Count > 0) {
            var errorMessages = Errors;
            Errors = [];
            throw new MicroserviceException(errorMessages, 400, "PARAMETER_VALIDATION_ERROR");
        }

        return requestParams;
    }

    private static void CheckRequiredParams(List<string> requestParamsList, List<string> requiredParams) {
        var unusedParams = requiredParams.Except(requestParamsList);

        if (unusedParams.Any()) {
            foreach (var unusedParam in unusedParams) {
                Errors.Add($"Required parameters not used. (Parameter '{unusedParam}')");
            }
        }
    }

    private static List<string> GetRequiredParamsList(Dictionary<string, ActionParams> parameters) {
        return parameters.Where(p => p.Value.Required).Select(p => p.Key).ToList();
    }

    private static bool IsEmail(string email) => Regex.IsMatch(email, ValidationPatterns.Email);

    private static bool IsPassword(string password) => Regex.IsMatch(password, ValidationPatterns.Password);
}
