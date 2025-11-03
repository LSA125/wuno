using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Users
{
    public interface IAppUserResolver
    {
        bool TryGet(out Guid userId);
    }
}
