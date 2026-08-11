using Microsoft.AspNetCore.Mvc;

namespace Bricker.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get() => Ok(new HealthResponse("Bricker API", "healthy"));
}

public sealed record HealthResponse(string Service, string Status);
