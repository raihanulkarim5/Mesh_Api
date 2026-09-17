using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mesh.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Unauthenticated - just proves the API is up and the pipeline (routing,
    /// DI, middleware) is wired correctly. Once this responds, the next step
    /// is confirming the DB connection with a migration.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get() => Ok(new { status = "ok", utc = DateTime.UtcNow });
}
