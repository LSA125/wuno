using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wuno.Application.Users;

namespace Wuno.Api.Controllers
{
    [ApiController]
    [Route("api/stats")]
    [AllowAnonymous]
    public class StatsController(IStatsService stats) : ControllerBase
    {
        private readonly IStatsService _stats = stats;

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetStats(Guid userId, CancellationToken ct)
        {
            var result = await _stats.GetUserStatsAsync(userId, ct);
            return Ok(result);
        }

        [HttpGet("{userId:guid}/ingame")]
        public async Task<IActionResult> GetInGameStats(Guid userId, CancellationToken ct)
        {
            var result = await _stats.GetInGameStatsAsync(userId, ct);
            return Ok(result);
        }
    }
}
