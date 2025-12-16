using Wuno.Application.Games.Util;

namespace Wuno.Application.Users
{
    public interface IStatsService
    {
        Task<UserStatsResponse> GetUserStatsAsync(Guid userId, CancellationToken ct);
        Task<InGameStatsResponse> GetInGameStatsAsync(Guid userId, CancellationToken ct);
    }
}
