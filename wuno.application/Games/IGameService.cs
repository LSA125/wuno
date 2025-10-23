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
        Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct);
        Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct);
        Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct);
        Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct);
        Task<bool> IsMatchEndAsync(Guid gameId, Guid playerId, CancellationToken ct);
        Task EndMatchAsync(Guid gameId, CancellationToken ct);
        Task<TurnState> StartRoundAsync(Guid gameId, CancellationToken ct);
        Task<bool> IsRoundEndAsync(Guid gameId, Guid roundId, CancellationToken ct);
        Task EndRoundAsync(Guid gameId, Guid roundId, Guid winnerId, CancellationToken ct);
        Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct);
        Task<RoundState> GetRoundAsync(Guid roundId, CancellationToken ct);
        Task<TurnState> GetTurnAsync(Guid turnId, CancellationToken ct);
        Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
        Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, SubmitWordRequest req, CancellationToken ct);
        TurnState StartTurn(Game game, Round round, char? prevAcceptedLetter, CancellationToken ct);
        Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct);
        Task DisconnectProtocolAsync(Guid gameId, Guid playerId, CancellationToken ct);
        Task LeaveGameAsync(Guid gameId, Guid playerId, CancellationToken ct);
        //true if advanced
        Task<bool> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct);
    }
}
