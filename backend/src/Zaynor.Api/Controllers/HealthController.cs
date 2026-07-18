using Microsoft.AspNetCore.Mvc;

namespace Zaynor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "Zaynor.Api" });
}
