using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games.Util;

namespace Wuno.Application.Games.Inheritance
{
    public interface IGameService
    {
        Task<Guid> GetGameId(string code, CancellationToken ct);
        Task ReadyAsync(Guid gameId, int seat, bool isReady, CancellationToken ct);
        Task<bool> AreAllPlayersReadyAsync(Guid gameId, CancellationToken ct);
        Task<NewGameResponse> StartNewGameAsync(NewGameRequest req, CancellationToken ct);
        Task<bool> MarkMatchAsStartedAsync(Guid gameId, CancellationToken ct);
        Task<TurnState> StartMatchAsync(Guid gameId, CancellationToken ct);
        Task<bool> IsMatchEndAsync(Guid gameId, CancellationToken ct);
        Task EndMatchAsync(Guid gameId, CancellationToken ct);
        Task<TurnState> StartRoundAsync(Guid gameId, CancellationToken ct);
        Task<bool> IsRoundEndAsync(Guid gameId, CancellationToken ct);
        Task EndRoundAsync(Guid gameId, Guid roundId, CancellationToken ct);
        Task<TurnState> StartTurnAsync(Guid gameId, CancellationToken ct);
        Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct);
        Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
        Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct);
        Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct);
        Task<SubmitWordResponse> SubmitWordAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct);
        Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct);
        Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct);
        Task LeaveGameAsync(Guid userId, CancellationToken ct);
        //true if advanced
        Task<bool> TimeoutAsync(Guid gameId, Guid turnId, CancellationToken ct);
        Task ForceEndGame(Guid gameId, CancellationToken ct);
    }
}
