using Microsoft.AspNetCore.Mvc;
using Wuno.Application;
using Wuno.Application.Games;
namespace Wuno.Api.Controllers
{

    [ApiController]
    [Route("api")]
    public sealed class GamesController : ControllerBase
    {
        private readonly IGameService _svc;
        public GamesController(IGameService svc)
        {
            _svc = svc;
        }
        [HttpPost("hotseat/new")]
        public async Task<IActionResult> New([FromBody] NewGameRequest request, CancellationToken cancellationToken)
        {
            var res = await _svc.StartNewGameAsync(request, cancellationToken);
            return Ok(res);
        }
        [HttpPost("games/{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var state = await _svc.GetGameStateAsync(id, ct);
            return state is null ? NotFound() : Ok(state);
        }
        [HttpPost("games/{id:guid}/submit")]
        public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitWordRequest req, CancellationToken ct)
        {
            var res = await _svc.SubmitWordAsync(id, req, ct);
            return Ok(res);
        }
    }
}
