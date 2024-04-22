using System.Text.Json;
using System.Text.RegularExpressions;
using AlfaMicroserviceMesh.Constants;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using AlfaMicroserviceMesh.Models.Errors;
using AlfaMicroserviceMesh.Models.Service.Handler;

namespace AlfaMicroserviceMesh.Validator;

public static class CustomValidator {
    private static List<object> Errors { get; set; } = [];

    public static async Task<object> ValidateParams(object requestParams, Dictionary<string, ActionParams> actionParams) {
        var paramsString = await requestParams.SerializeAsync();
        var paramsDict = await paramsString.DeserializeAsync<Dictionary<string, object>>();
        var parameters = JsonDocument.Parse(paramsString);

        CheckRequiredParams([.. paramsDict.Keys], GetRequiredParamsList(actionParams));

        foreach (var param in parameters.RootElement.EnumerateObject()) {
            string requestParam = param.Value.ToString();
            string requestParamType = param.Value.ValueKind.ToString();

            if (actionParams.TryGetValue(param.Name, out ActionParams? actionParam)) {
                var actionParamType = actionParam.Type;
                var allowedValues = actionParam.Allowed;

                if (allowedValues is not null) {
                    actionParamType = "String";
                    if (!allowedValues.Contains(requestParam)) {
                        Errors.Add(new ErrorValidationData {
                            Field = param.Name,
                            Value = requestParam,
                            Message = ErrorMessages.NotAllowed + string.Join(", ", allowedValues),
                        });
                    }
                }
                if (actionParamType is "Email") {
                    if (IsEmail(requestParam) is false) {
                        Errors.Add(new ErrorValidationData {
                            Field = param.Name,
                            Value = requestParam,
                            Message = ErrorMessages.EmailFormat
                        });
                    }
                }
                else if (actionParamType is "Password") {
                    if (IsPassword(requestParam) is false) {
                        Errors.Add(new ErrorValidationData {
                            Field = param.Name,
                            Value = requestParam,
                            Message = ErrorMessages.PasswordFormat
                        });
                    }
                }
                else if (actionParamType != requestParamType) {
                    Errors.Add(new ErrorValidationData {
                        Field = param.Name,
                        Value = int.TryParse(requestParam, out int value) ? value : requestParam,
                        Message = ErrorMessages.Type + $"'{actionParamType}'",
                    });
                }
            }
        }

        if (Errors.Count > 0) {
            var errorMessages = Errors;
            Errors = [];
            throw new MicroserviceException(errorMessages, ErrorTypes.ValidationError);
        }

        return requestParams;
    }

    private static void CheckRequiredParams(List<string> requestParamsList, List<string> requiredParams) {
        var unusedParams = requiredParams.Except(requestParamsList);

        if (unusedParams.Any()) {
            foreach (var unusedParam in unusedParams) {
                Errors.Add(new ErrorValidationData {
                    Field = unusedParam,
                    Message = ErrorMessages.Required,
                });
            }
        }
    }

    private static List<string> GetRequiredParamsList(Dictionary<string, ActionParams> parameters)
        => parameters.Where(p => p.Value.Required).Select(p => p.Key).ToList();

    private static bool IsEmail(string email)
        => Regex.IsMatch(email, ValidationPatterns.Email);

    private static bool IsPassword(string password)
        => Regex.IsMatch(password, ValidationPatterns.Password);
}
