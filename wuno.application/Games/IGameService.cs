using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public interface IGameService
    {
        Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct);
        Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, SubmitWordRequest req, CancellationToken ct);
        Task<object?> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
    }
}
