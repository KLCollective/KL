using Microsoft.AspNetCore.Mvc;

namespace KinkLinkServer.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(new { status = "healthy" });
}