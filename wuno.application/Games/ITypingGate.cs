using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games
{
    public interface ITypingGate
    {
        public bool tryAllow(string key, TimeSpan interval);
    }
}
