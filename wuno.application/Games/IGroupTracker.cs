using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public interface IGroupTracker
    {
        void Add(string connectionId, Guid group);
        void Remove(string connectionId, Guid group);
        IReadOnlyCollection<Guid> GetGroups(string connectionId);
        void Clear(string connectionId);
    }
}
