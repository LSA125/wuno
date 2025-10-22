using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public sealed class GroupTracker
    {
        private readonly ConcurrentDictionary<string, HashSet<Guid>> _map = new();
        public void Add(string connectionId, Guid group)
        {
            var groups = _map.GetOrAdd(connectionId, _ => new HashSet<Guid>());
            lock (groups)
            {
                groups.Add(group);
            }
        }
        public void Remove(string connectionId, Guid group)
        {
            if (_map.TryGetValue(connectionId, out var groups))
            {
                lock (groups)
                {
                    groups.Remove(group);
                    if (groups.Count == 0)
                    {
                        _map.TryRemove(connectionId, out _);
                    }
                }
            }
        }
        public IReadOnlyCollection<Guid> GetGroups(string connectionId)
        {
            if (_map.TryGetValue(connectionId, out var groups))
            {
                lock (groups)
                {
                    return groups.ToList().AsReadOnly();
                }
            }
            return [];
        }
        public void Clear(string connectionId)
        {
            _map.TryRemove(connectionId, out _);
        }
    }
}
