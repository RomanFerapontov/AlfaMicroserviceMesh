using AlfaMicroserviceMesh.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using Serilog;
using Microsoft.Extensions.Caching.Memory;
using AlfaMicroserviceMesh.TokenService;
using AlfaMicroserviceMesh.Registry;
using AlfaMicroserviceMesh.Models.Errors;
using AlfaMicroserviceMesh.Constants;

namespace AlfaMicroserviceMesh.Controllers;

[ApiController]
public class HandlersController(
    IHttpContextAccessor httpContextAccessor,
    IJwtTokenService tokenService, IMemoryCache cache) : ControllerBase {

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IJwtTokenService _tokenService = tokenService;
    private readonly IMemoryCache _cache = cache;

    [Route("{*any}")]
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> AsyncHandler() {
        try {
            HttpContext httpContext = _httpContextAccessor.HttpContext!;

            var headers = httpContext.Request.Headers;
            string method = httpContext.Request.Method;
            string path = httpContext.Request.Path.ToString();
            string cacheKey = $"{path}?{httpContext.Request.QueryString}";

            if (!Regex.IsMatch(path, ValidationPatterns.Path))
                throw new MicroserviceException("Invalid URL", ErrorTypes.ValidationError);

            string[] splittedPath = path.Split("/");
            string targetNode = splittedPath[1];
            string targetAction = splittedPath[2];

            var requestParams = await GetParams(httpContext);
            var targetInstance = Services.GetNodeInstanceUid(targetNode);
            var actionData = await Services.GetActionData(targetNode, targetInstance, targetAction);

            List<string>? roles = actionData.Access;

            if (roles is not null) {
                if (!headers.ContainsKey("Authorization"))
                    throw new MicroserviceException("Authorization token have not provided", ErrorTypes.Unauthorized);

                string token = headers.Authorization!.ToString().Split(" ")[1];

                var accessData = await _tokenService.GetAccessData(token);

                if (!roles.Contains(accessData["role"]))
                    throw new MicroserviceException("Permission denied", ErrorTypes.Forbidden);

                requestParams["Access"] = new ClaimData {
                    Uid = accessData["uid"],
                    Role = accessData["role"],
                };
            }

            if (_cache.TryGetValue(cacheKey, out object? cachedResponse)) {
                return StatusCode(200, cachedResponse);
            }

            var response = await Services.Call(targetNode, targetAction, requestParams, targetInstance);

            if (actionData.Caching is true) {
                _cache.Set(cacheKey, response?.Data, new MemoryCacheEntryOptions {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                });
            };

            if (targetAction is "SignIn" || targetAction is "Login") {
                var claims = await response!.Data!.ConvertToModel<ClaimData>();

                return StatusCode(200, new {
                    response?.Data,
                    jwt = _tokenService.CreateToken(claims!)
                });
            }

            return StatusCode(200, response?.Data);
        }
        catch (Exception exception) {
            Log.Error(exception.Message);

            return exception is MicroserviceException MSException ?
                StatusCode(MSException.Info.Status, MSException.Info) :
                StatusCode(500, new ErrorData { Error = exception.Message });
        }
    }

    private static async Task<Dictionary<string, object>> GetParams(HttpContext httpContext) {
        var request = httpContext.Request;

        Dictionary<string, object> parameters = [];

        string body = request.ContentLength.HasValue && request.ContentLength > 0 ?
            await new StreamReader(request.Body).ReadToEndAsync() : "{}";

        var bodyParams = await body.DeserializeAsync<Dictionary<string, object>>();

        foreach (var queryParam in request.Query) {
            if (int.TryParse(queryParam.Value, out int id)) {
                parameters[queryParam.Key] = id;
            }
            else {
                parameters[queryParam.Key] = queryParam.Value[0]!;
            }
        }

        foreach (var param in bodyParams) {
            parameters[param.Key] = param.Value!;
        }

        return parameters;
    }
}