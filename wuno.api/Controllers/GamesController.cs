using Microsoft.AspNetCore.Mvc;
using Wuno.Application;
using Wuno.Application.Games;
namespace Wuno.Api.Controllers
{

    [ApiController]
    [Route("api/games")]
    public sealed class GamesController : ControllerBase
    {
        private readonly IGameService _svc;
        public GamesController(IGameService svc)
        {
            _svc = svc;
        }
        [HttpPost("new")]
        public async Task<IActionResult> New([FromBody] NewGameRequest request, CancellationToken cancellationToken)
        {
            NewGameResponse res = await _svc.StartNewGameAsync(request, cancellationToken);

            return Ok(res);
        }
        [HttpPost("id/{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var state = await _svc.GetGameStateAsync(id, ct);
            return state is null ? NotFound() : Ok(state);
        }
    }
}
