using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.infrastructure;

namespace Wuno.Application.Games
{
    public interface IGameService
    {
        Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct);
        Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, SubmitWordRequest req, CancellationToken ct);
        Task<object?> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
        Task<List<(Guid gameId, Guid turnId)>> FindOverdueAsync(AppDbContext db, CancellationToken ct);
        Task TimeoutAndAdvanceAsync(AppDbContext db, Guid gameId, Guid turnId, CancellationToken ct);
    }
}
