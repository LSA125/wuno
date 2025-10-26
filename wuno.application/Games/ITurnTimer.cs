using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public interface ITurnTimer
    {
        public bool Schedule(Guid gameId, Guid turnId, DateTime dueAtUTC, Func<Guid, Guid, Task> Broadcast);
        public void Cancel(Guid turnId);
    }
}
