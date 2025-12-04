using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Application.Games.Implementation
{
    public sealed class GroupTracker : IGroupTracker
    {
        private readonly ConcurrentDictionary<string, PlayerSession> _byConn = new();
        private readonly ConcurrentDictionary<Guid, int> _refCounts = new();

        public void Add(string connId, PlayerSession ps)
        {
            _byConn[connId] = ps;
            _refCounts.AddOrUpdate(ps.PlayerId, 1, (_, n) => n + 1);
        }

        // returns (playerId, gameId, seat, isLast)
        public bool Remove(string connId, out PlayerSession ps, out bool isLast)
        {
            isLast = false;
            if (!_byConn.TryRemove(connId, out var e))
            { ps = default!; return false; }

            ps = e;

            var after = _refCounts.AddOrUpdate(ps.PlayerId, 0, (_, n) => Math.Max(0, n - 1));
            if (after == 0) { _refCounts.TryRemove(ps.PlayerId, out _); isLast = true; }
            return true;
        }

        public bool TryGet(string connId, out PlayerSession ps)
        {
            if (_byConn.TryGetValue(connId, out var e))
            { ps = e; return true; }
            ps = default!; return false;
        }
    }
}
