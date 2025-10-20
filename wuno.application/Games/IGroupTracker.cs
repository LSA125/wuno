using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public interface IGroupTracker
    {
        void Add(string connectionId, string group);
        void Remove(string connectionId, string group);
        IReadOnlyCollection<string> GetGroups(string connectionId);
        void Clear(string connectionId);
    }
}
