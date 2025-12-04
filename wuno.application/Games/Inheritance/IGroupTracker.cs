namespace Wuno.Application.Games.Inheritance
{
    public record PlayerSession(Guid GameId, Guid PlayerId, int Seat, Guid UserId);
    public interface IGroupTracker
    {
        void Add(string connectionId, PlayerSession session);
        bool TryGet(string connectionId, out PlayerSession session);
        bool Remove(string connectionId, out PlayerSession session, out bool isLast);
    }
}
