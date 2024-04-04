using AlfaMicroserviceMesh.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AlfaMicroserviceMesh.Validator;
using System.Text.RegularExpressions;
using AlfaMicroserviceMesh.Services;
using AlfaMicroserviceMesh.Exceptions;
using AlfaMicroserviceMesh.Extentions;
using Serilog;

namespace AlfaMicroserviceMesh.Utils;

[ApiController]
public class UniversalController(
    IHttpContextAccessor httpContextAccessor,
    ITokenService tokenService) : ControllerBase {

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ITokenService _tokenService = tokenService;

    [Route("{*any}")]
    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> AsyncHandler() {
        try {
            HttpContext httpContext = _httpContextAccessor.HttpContext!;

            var headers = httpContext.Request.Headers;
            string path = httpContext.Request.Path.ToString();

            if (!Regex.IsMatch(path, ValidationPatterns.Path))
                throw new MicroserviceException(["Invalid URL"], 400, "INVALID_URL");

            string[] splittedPath = path.Split("/");
            string targetNode = splittedPath[1];
            string targetAction = splittedPath[2];

            var requestParams = await GetParams(httpContext);

            string targetInstance = NodesRegistry.GetNodeInstanceUid(targetNode);

            List<string>? roles = NodesRegistry.GetActionData(targetNode, targetInstance, targetAction).Access;

            if (roles![0] != "ALL") {
                if (!headers.ContainsKey("Authorization"))
                    throw new MicroserviceException(["Authorization token have not provided"], 401, "AUTHORIZATION_ERROR");

                string token = headers.Authorization!.ToString().Split(" ")[1];

                var accessData = await _tokenService.GetAccessData(token);

                if (!roles.Contains(accessData["role"]))
                    throw new MicroserviceException(["Permission denied"], 403, "AUTHORIZATION_ERROR");

                requestParams["Access"] = new ClaimData {
                    Uid = accessData["uid"],
                    Role = accessData["role"],
                };
            }
            
            var result = await NodesRegistry.Call(targetNode, targetAction, requestParams, targetInstance);

            if (targetAction == "SignUp" || targetAction == "SignIn") {
                var claims = await result!.ConvertToModel<ClaimData>();

                result = new {
                    UserData = result,
                    jwt = _tokenService.CreateToken(claims!)
                };
            }

            if (result == null)
                throw new MicroserviceException(["Request Timeout"]);

            return StatusCode(200, new { Status = 200, Data = result });
        }
        catch (Exception exception) {
            Log.Error(exception.Message);

            return exception is MicroserviceException MSException ?
                StatusCode(MSException.Info.Status, MSException.Info) :
                StatusCode(500, new ErrorData { Errors = [exception.Message] });
        }
    }

    [HttpGet]
    [Route("api")]
    public ActionResult GetNodeRegistryInfo() => Ok(NodesRegistry._nodes);

    private async Task<Dictionary<string, object>> GetParams(HttpContext httpContext) {
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