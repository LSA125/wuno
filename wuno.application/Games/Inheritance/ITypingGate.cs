using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Application.Games.Inheritance
{
    public interface ITypingGate
    {
        public bool tryAllow(string key, TimeSpan interval);
    }
}
