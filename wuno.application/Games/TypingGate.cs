using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Application.Games
{
    public sealed class TypingGate : ITypingGate
    {
        private readonly ConcurrentDictionary<string, long> _lastTicks = new();
        public bool tryAllow(string key, TimeSpan interval)
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            var intervalTicks = interval.Ticks;

            while (true)
            {
                var prev = _lastTicks.GetOrAdd(key, 0);
                if (nowTicks - prev < intervalTicks)
                {
                    return false;
                }
                if (_lastTicks.TryUpdate(key, nowTicks, prev))
                {
                    return true;
                }
            }
        }
    }
}
