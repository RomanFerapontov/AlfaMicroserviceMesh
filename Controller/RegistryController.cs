using AlfaMicroserviceMesh.Registry;
using Microsoft.AspNetCore.Mvc;

namespace AlfaMicroserviceMesh.Controller;

[ApiController]
public class RegistryController : ControllerBase {
    [HttpGet]
    [Route("registry")]
    public ActionResult GetNodeRegistryInfo() => Ok(Services._services);

    [HttpGet]
    [Route("registry/{service}")]
    public ActionResult GetNodeRegistryInfo(string service) {
        var services = Services._services;
        if (!services.ContainsKey(service)) return NotFound("Service not found");
        
        return Ok(Services._services[service]);
    }
}