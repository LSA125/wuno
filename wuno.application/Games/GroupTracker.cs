using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public sealed class GroupTracker : IGroupTracker
    {
        private readonly ConcurrentDictionary<string, PlayerSession> _map = new();
        public void Add(string connectionId, PlayerSession ps)
        {
            _map[connectionId] = ps;
        }
        public void Remove(string connectionId)
        {
            if(_map.TryGetValue(connectionId, out var ps))
            {
                _map.TryRemove(connectionId, out _);
            }
        }
        public bool TryGet(string connectionId, out PlayerSession session)
        {
            if(_map.TryGetValue(connectionId, out var ps))
            {
                session = ps;
                return true;
            }
            session = default!;
            return false;
        }
        public IEnumerable<PlayerSession> GetConnectionsForGame(Guid gameId)
        {
            return _map.Values.Where(ps => ps.GameId == gameId);
        }
        public void Clear(string connectionId)
        {
            _map.TryRemove(connectionId, out _);
        }
    }
}
