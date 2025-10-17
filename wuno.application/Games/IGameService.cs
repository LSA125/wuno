using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.infrastructure;

namespace Wuno.Application.Games
{
    public interface IGameService
    {
        Task<Guid> GetGameId(string code, CancellationToken ct);
        Task<List<Player>> GetPlayers(Guid gameId, CancellationToken ct);
        Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct);
        Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct);
        Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, SubmitWordRequest req, CancellationToken ct);
        Task<object?> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
        Task<List<(Guid gameId, Guid turnId)>> FindOverdueAsync(AppDbContext db, CancellationToken ct);
        Task TimeoutAndAdvanceAsync(AppDbContext db, Guid gameId, Guid turnId, CancellationToken ct);
    }
}
