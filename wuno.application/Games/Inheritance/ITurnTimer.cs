using Wuno.Application.Games.Util;

namespace Wuno.Application.Games.Inheritance
{
    public interface ITurnTimer
    {
        public bool Schedule(Guid gameId, Guid turnId, DateTime dueAtUTC, Func<GameState, Task> Broadcast);
        public void Cancel(Guid turnId);
    }
}
