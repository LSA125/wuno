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
        Task<List<PlayerState>> GetPlayersAsync(Guid gameId, CancellationToken ct);
        Task<GameState> GetGameStateAsync(Guid gameId, CancellationToken ct); // compact state for UI
        Task<int> GetCurrentSeatAsync(Guid gameId, CancellationToken ct);
        Task<GameCodeResponse> GetUserActiveGameCodeAsync(Guid userId, CancellationToken ct);
        Task<JoinGameResponse> JoinGameAsync(Guid gameId, Guid userId, CancellationToken ct);
        Task<ProcessTurnOutcome> ProcessTurnAsync(Guid gameId, Guid roundId, Guid turnId, Guid playerId, int seat, string word, CancellationToken ct);
        Task<(Guid gameId, List<PlayerState> players)> DisconnectProtocolAsync(Guid playerId, CancellationToken ct);
        Task LeaveGameAsync(Guid userId, CancellationToken ct);
        Task<GameState?> TimeoutAndAdvanceAsync(Guid gameId, Guid turnId, CancellationToken ct);
        Task ForceEndGame(Guid gameId, CancellationToken ct);
    }
}
