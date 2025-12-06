using Microsoft.AspNetCore.Mvc;

namespace Wuno.Api.Controllers
{
    [ApiController]
    [Route("health")]
    public sealed class HealthController : ControllerBase
    {
        [HttpGet("live")]
        public IActionResult Live() => Ok(new { ok = true });

        [HttpGet("ready")]
        public IActionResult Ready() => Ok(new { ok = true });
    }
}